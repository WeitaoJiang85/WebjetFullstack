using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebjetAPI.Models;
using DotNetEnv; // Import DotNetEnv to load .env file

namespace WebjetAPI.Services
{
    /// <summary>
    /// Background service that periodically fetches detailed movie data from providers and stores it in Redis.
    /// </summary>
    public class MovieDetailCacheService : BackgroundService
    {
        private readonly ILogger<MovieDetailCacheService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDatabase? _cache;
        private readonly IConfiguration _configuration;
        private readonly string _apiToken;

        public MovieDetailCacheService(
            ILogger<MovieDetailCacheService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IConnectionMultiplexer? redis)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cache = redis?.GetDatabase();
            _configuration = configuration;

            // Load API Token from .env file
            Env.Load();
            _apiToken = Env.GetString("WEBJET_API_TOKEN")
                        ?? throw new ArgumentNullException("API Token is missing from .env file");
        }

        /// <summary>
        /// Background service execution loop.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting scheduled movie detail data cache refresh");

                await RunDetailUpdateAsync();

                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        /// <summary>
        /// Public method that triggers an update of movie details, used by MovieCacheService.
        /// </summary>
        public async Task RunDetailUpdateAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var cachedMovies = await FetchMoviesFromCache();

                _logger.LogInformation($"Found {cachedMovies["cinemaworld"].Count} movies in cinemaworld cache");
                _logger.LogInformation($"Found {cachedMovies["filmworld"].Count} movies in filmworld cache");

                var failedRequests = new List<(string Provider, string MovieId)>();

                foreach (var provider in cachedMovies.Keys)
                {
                    foreach (var movie in cachedMovies[provider])
                    {
                        if (!string.IsNullOrEmpty(movie.ID))
                        {
                            bool success = await FetchAndCacheMovieDetails(client, provider, movie.ID);
                            if (!success)
                            {
                                failedRequests.Add((provider, movie.ID));
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Skipping movie with null ID from {provider}");
                        }
                    }
                }

                // Retry fetching failed movie details once
                if (failedRequests.Count > 0)
                {
                    _logger.LogWarning($"Retrying {failedRequests.Count} failed movie detail fetches");

                    foreach (var (provider, movieId) in failedRequests)
                    {
                        await FetchAndCacheMovieDetails(client, provider, movieId);
                    }
                }

                // Merge detailed movie data
                await MergeMovieDetails();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie detail cache");
            }
        }

        /// <summary>
        /// Fetches cached movie lists from Redis.
        /// </summary>
        private async Task<Dictionary<string, List<Movie>>> FetchMoviesFromCache()
        {
            var cachedMovies = new Dictionary<string, List<Movie>>
            {
                { "cinemaworld", new List<Movie>() },
                { "filmworld", new List<Movie>() }
            };

            try
            {
                if (_cache != null)
                {
                    var cinemaData = await _cache.StringGetAsync("cinemaworld_movies");
                    var filmData = await _cache.StringGetAsync("filmworld_movies");

                    cachedMovies["cinemaworld"] = !string.IsNullOrEmpty(cinemaData)
                        ? JsonSerializer.Deserialize<List<Movie>>(cinemaData!) ?? new List<Movie>()
                        : new List<Movie>();

                    cachedMovies["filmworld"] = !string.IsNullOrEmpty(filmData)
                        ? JsonSerializer.Deserialize<List<Movie>>(filmData!) ?? new List<Movie>()
                        : new List<Movie>();

                    _logger.LogInformation("Successfully fetched movie lists from Redis.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch cached movies from Redis.");
            }

            return cachedMovies;
        }

        /// <summary>
        /// Fetches and caches detailed movie data for a given provider and movie ID.
        /// </summary>
        private async Task<bool> FetchAndCacheMovieDetails(HttpClient client, string provider, string movieId)
        {
            string cacheKey = $"{provider}_movie_details:{movieId}";
            string? baseUrl = _configuration[$"WebjetAPI:BaseUrls:{provider}"];

            if (string.IsNullOrEmpty(baseUrl))
            {
                _logger.LogWarning($"Base URL for {provider} is missing. Skipping movie details fetch.");
                return false;
            }

            var movieUrl = $"{baseUrl}/movie/{movieId}";
            _logger.LogInformation($"Fetching movie details from {movieUrl}");

            int retryCount = 3;
            int delay = 2000; // Initial delay 2 seconds

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, movieUrl);
                    request.Headers.Add("x-access-token", _apiToken);

                    HttpResponseMessage response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        var movieDetail = JsonSerializer.Deserialize<MovieDetail>(json);

                        if (movieDetail != null)
                        {
                            movieDetail.Provider = provider;
                            json = JsonSerializer.Serialize(movieDetail);

                            await _cache!.StringSetAsync(cacheKey, json, TimeSpan.FromDays(1)); // Cache the data for 1 day
                            _logger.LogInformation($"Cached details for {movieId} from {provider}");
                            return true;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to fetch {movieId} from {provider} (Attempt {attempt}/{retryCount}): {response.StatusCode}");
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError($"HTTP request error for {movieId} from {provider} (Attempt {attempt}/{retryCount}): {ex.Message}");
                }

                if (attempt < retryCount)
                {
                    _logger.LogWarning($"Retrying fetch for {movieId} from {provider} in {delay / 1000}s...");
                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff (2s -> 4s -> 8s)
                }
            }

            // If all attempts fail, use existing cache if available
            var cachedData = await _cache.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
                _logger.LogWarning($"Using existing cached data for {movieId} from {provider} after all retries failed.");
                return true;
            }

            _logger.LogError($"Failed to fetch and cache {movieId} from {provider} after {retryCount} attempts.");
            return false;
        }

        /// <summary>
        /// Merges detailed movie data from different providers and stores it in Redis.
        /// </summary>
        private async Task MergeMovieDetails()
        {
            if (_cache == null)
            {
                _logger.LogWarning("Redis cache is unavailable. Skipping movie detail merging.");
                return;
            }

            var mergedMoviesData = await _cache.StringGetAsync("merged_movies");

            if (mergedMoviesData.IsNullOrEmpty)
            {
                _logger.LogWarning("No merged movie data found in Redis. Skipping detail merging.");
                return;
            }

            var mergedMovies = JsonSerializer.Deserialize<List<MergedMovie>>(mergedMoviesData!) ?? new List<MergedMovie>();

            _logger.LogInformation($"Loaded {mergedMovies.Count} merged movies from Redis.");

            var detailedMovies = new List<MergedMovieDetail>();

            foreach (var movie in mergedMovies)
            {
                // Fetch detailed movie data from Redis using both IDs
                var details = new List<(MovieDetail? Movie, string Provider, string MovieID)>();

                foreach (var movieId in movie.IDs)
                {
                    var provider = movieId.StartsWith("cw") ? "cinemaworld" : "filmworld";
                    var redisData = await _cache.StringGetAsync($"{provider}_movie_details:{movieId}");

                    if (!redisData.IsNullOrEmpty)
                    {
                        var movieDetail = JsonSerializer.Deserialize<MovieDetail>(redisData!);
                        if (movieDetail != null)
                        {
                            details.Add((movieDetail, provider, movieId));
                        }
                    }
                }

                _logger.LogInformation($"Movie: {movie.Title} | Fetched {details.Count} details from Redis.");

                if (details.Count == 0)
                {
                    _logger.LogWarning($"Skipping {movie.Title} - No detailed movie data found in Redis.");
                    continue; // Skip if no detailed data
                }

                // Determine first and second movie based on price
                var sortedMovies = details.OrderBy(m => m.Movie!.Price).ToList();
                var first = sortedMovies[0];
                var second = sortedMovies.Count > 1 ? sortedMovies[1] : (null, "Unknown", "N/A");

                var mergedDetail = new MergedMovieDetail
                {
                    Title = first.Movie?.Title ?? second.Movie?.Title ?? string.Empty,
                    Year = first.Movie?.Year ?? second.Movie?.Year ?? "Unknown",
                    Rated = first.Movie?.Rated ?? second.Movie?.Rated ?? "N/A",
                    Released = first.Movie?.Released ?? second.Movie?.Released ?? "N/A",
                    Runtime = first.Movie?.Runtime ?? second.Movie?.Runtime ?? "N/A",
                    Genre = first.Movie?.Genre ?? second.Movie?.Genre ?? "Unknown",
                    Director = first.Movie?.Director ?? second.Movie?.Director ?? "Unknown",
                    Writer = first.Movie?.Writer ?? second.Movie?.Writer ?? "Unknown",
                    Actors = first.Movie?.Actors ?? second.Movie?.Actors ?? "Unknown",
                    Plot = first.Movie?.Plot ?? second.Movie?.Plot ?? "N/A",
                    Language = first.Movie?.Language ?? second.Movie?.Language ?? "Unknown",
                    Country = first.Movie?.Country ?? second.Movie?.Country ?? "Unknown",
                    Awards = first.Movie?.Awards ?? second.Movie?.Awards ?? "N/A",
                    Poster = first.Movie?.Poster ?? second.Movie?.Poster ?? "https://via.placeholder.com/300x450?text=No+Image",
                    Metascore = first.Movie != null && second.Movie != null
                        ? ((int.TryParse(first.Movie.Metascore, out var pm) && int.TryParse(second.Movie.Metascore, out var sm))
                            ? ((pm + sm) / 2).ToString()
                            : first.Movie.Metascore ?? second.Movie.Metascore ?? "N/A")
                        : first.Movie?.Metascore ?? second.Movie?.Metascore ?? "N/A",
                    Rating = first.Movie != null && second.Movie != null
                        ? ((decimal.TryParse(first.Movie.Rating, out var pr) && decimal.TryParse(second.Movie.Rating, out var sr))
                            ? Math.Round((pr + sr) / 2, 1)
                            : decimal.Parse(first.Movie?.Rating ?? second.Movie?.Rating ?? "0.0"))
                        : decimal.Parse(first.Movie?.Rating ?? second.Movie?.Rating ?? "0.0"),
                    Votes = first.Movie != null && second.Movie != null
                        ? ((int.TryParse(first.Movie.Votes.Replace(",", ""), out var pv) && int.TryParse(second.Movie.Votes.Replace(",", ""), out var sv))
                            ? pv + sv
                            : int.Parse(first.Movie?.Votes.Replace(",", "") ?? second.Movie?.Votes.Replace(",", "") ?? "0"))
                        : int.Parse(first.Movie?.Votes.Replace(",", "") ?? second.Movie?.Votes.Replace(",", "") ?? "0"),
                    RawID = movie.RawID,
                    Type = first.Movie?.Type ?? second.Movie?.Type ?? "Unknown",
                    FirstID = first.Movie?.ID ?? "",
                    SecondID = second.Movie?.ID ?? "",
                    FirstPrice = first.Movie != null ? first.Movie.Price : -1m,
                    FirstProvider = first.Movie?.Provider ?? "Unknown",
                    SecondPrice = second.Movie != null ? second.Movie.Price : 0m,
                    SecondProvider = second.Movie?.Provider ?? ""
                };

                detailedMovies.Add(mergedDetail);
            }

            if (detailedMovies.Count > 0)
            {
                await _cache.StringSetAsync("merged_movie_details", JsonSerializer.Serialize(detailedMovies), TimeSpan.FromDays(1));
                _logger.LogInformation($"Merged movie details updated in Redis with {detailedMovies.Count} entries.");
            }
            else
            {
                _logger.LogWarning("No merged movie details were generated. The cache was not updated.");
            }
        }
    }
}
