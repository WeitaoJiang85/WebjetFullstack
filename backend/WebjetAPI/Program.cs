using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using WebjetAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json (Render Secret File)
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("/etc/secrets/appsettings.json", optional: true, reloadOnChange: true) // Load from Render Secret Files
    .AddEnvironmentVariables() // Also allow environment variables (fallback)
    .Build();

builder.Configuration.AddConfiguration(config);

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(config)
    .WriteTo.Console()
    .WriteTo.File("logs/webjetapi-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpClientFactory for handling external API requests with increased timeout
builder.Services.AddHttpClient("WebjetAPIClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // Increase timeout to 5 minutes
});

// Load API Token from environment variable
var apiToken = Environment.GetEnvironmentVariable("WEBJET_API_TOKEN") 
               ?? throw new Exception("API Token is missing from environment variables");

// Load Redis configuration
var redisHost = config["Redis:Host"];
var redisPort = config["Redis:Port"];
var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD"); // Load from environment variables

if (string.IsNullOrEmpty(redisHost) || string.IsNullOrEmpty(redisPort))
{
    throw new Exception("Redis host or port is missing in configuration.");
}

if (string.IsNullOrEmpty(redisPassword))
{
    throw new Exception("Redis password is missing in environment variables.");
}

// 拼接 Redis 连接字符串
var redisConnection = $"{redisHost}:{redisPort},password={redisPassword},ssl=false,abortConnect=false";

try
{
    Log.Information("Connecting to Redis at {RedisConfig}", redisConnection);
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
}
catch (Exception ex)
{
    Log.Error("Failed to connect to Redis: {ErrorMessage}", ex.Message);
    throw;
}

// Register services
builder.Services.AddSingleton<MovieDetailCacheService>();
builder.Services.AddHostedService<MovieCacheService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MovieDetailCacheService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger for both Development and Production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Webjet API V1");
});

// Middleware for authorization
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/healthz", () => Results.Ok("Healthy"));

// Get PORT from environment variable (default: 8080 for Render)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://*:{port}");

// Log application start message
Log.Information("Webjet API has started successfully.");
Log.Information("Running on port: {Port}", port);
Log.Information("Swagger UI available at: http://localhost:{Port}/swagger", port);

// Run the application
app.Run();
