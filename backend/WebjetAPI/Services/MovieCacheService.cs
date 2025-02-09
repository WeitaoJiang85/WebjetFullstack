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

namespace WebjetAPI.Services;

/// <summary>
/// Background service that periodically fetches basic movie data from providers and stores it in Redis.
/// </summary>
public class MovieCacheService : BackgroundService
{
    private readonly ILogger<MovieCacheService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IDatabase? _cache;
    private readonly MovieDetailCacheService _movieDetailCacheService;
    private readonly string _cinemaWorldBaseUrl;
    private readonly string _filmWorldBaseUrl;
    private readonly string _apiToken;

    public MovieCacheService(
        ILogger<MovieCacheService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IConnectionMultiplexer? redis,
        MovieDetailCacheService movieDetailCacheService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _cache = redis?.GetDatabase();
        _movieDetailCacheService = movieDetailCacheService;

        _cinemaWorldBaseUrl = _configuration["WebjetAPI:BaseUrls:Cinemaworld"] ?? throw new ArgumentNullException("Cinemaworld API URL is missing");
        _filmWorldBaseUrl = _configuration["WebjetAPI:BaseUrls:Filmworld"] ?? throw new ArgumentNullException("Filmworld API URL is missing");
        _apiToken = _configuration["WebjetAPI:ApiToken"] ?? throw new ArgumentNullException("API Token is missing");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Starting movie data cache refresh");

            try
            {
                var client = _httpClientFactory.CreateClient();
                var movies = await FetchMoviesFromApi(client);

                if (_cache != null)
                {
                    if (movies.TryGetValue("Cinemaworld", out var cinemaMovies))
                    {
                        var cinemaCacheData = JsonSerializer.Serialize(cinemaMovies);
                        await _cache.StringSetAsync("cinemaworld_movies", cinemaCacheData, TimeSpan.FromMinutes(10));
                        _logger.LogInformation("Updated Cinemaworld movie data in cache");
                    }

                    if (movies.TryGetValue("Filmworld", out var filmMovies))
                    {
                        var filmCacheData = JsonSerializer.Serialize(filmMovies);
                        await _cache.StringSetAsync("filmworld_movies", filmCacheData, TimeSpan.FromMinutes(10));
                        _logger.LogInformation("Updated Filmworld movie data in cache");
                    }
                }
                else
                {
                    _logger.LogWarning("Redis cache is unavailable");
                }

                // Trigger detailed movie data update after refreshing movie cache
                await _movieDetailCacheService.RunDetailUpdateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie cache");
            }

            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }

    private async Task<Dictionary<string, List<Movie>>> FetchMoviesFromApi(HttpClient client)
    {
        var allMovies = new Dictionary<string, List<Movie>>
        {
            { "Cinemaworld", new List<Movie>() },
            { "Filmworld", new List<Movie>() }
        };

        var providers = new Dictionary<string, string>
        {
            { "Cinemaworld", _cinemaWorldBaseUrl },
            { "Filmworld", _filmWorldBaseUrl }
        };

        foreach (var provider in providers)
        {
            try
            {
                var moviesUrl = $"{provider.Value}/movies";
                _logger.LogInformation($"Fetching movies from {provider}");
                var request = new HttpRequestMessage(HttpMethod.Get, moviesUrl);
                request.Headers.Add("x-access-token", _apiToken);
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var movies = JsonSerializer.Deserialize<MovieResponse>(await response.Content.ReadAsStringAsync())?.Movies ?? new List<Movie>();
                allMovies[provider.Key] = movies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to fetch movies from {provider}, keeping previous cache.");
            }
        }

        return allMovies;
    }
}
