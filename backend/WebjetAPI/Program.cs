using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using WebjetAPI.Services;
using DotNetEnv; // Import DotNetEnv to load .env file

var builder = WebApplication.CreateBuilder(args);

// Load .env file for environment variables
if (File.Exists("/etc/secrets/.env"))
{
    Env.Load("/etc/secrets/.env"); // Load from Render's Secret Files
}
else
{
    Env.Load(); // Load from local .env file
}

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webjetapi-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpClientFactory with increased timeout for API requests
builder.Services.AddHttpClient("WebjetAPIClient", client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Load API Token from .env file
var apiToken = Env.GetString("WEBJET_API_TOKEN") 
               ?? throw new Exception("API Token is missing from .env file");

// Load Redis connection string from .env file
var redisConnection = Env.GetString("REDIS_CONNECTION_STRING") 
                      ?? throw new Exception("Redis connection string is missing from .env file");

try
{
    Log.Information("Connecting to Redis at {RedisConfig}", redisConnection);
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
}
catch (Exception ex)
{
    Log.Error("Failed to connect to Redis: {ErrorMessage}", ex.Message);
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null); // Prevents API crash if Redis fails
}

// Register services
builder.Services.AddSingleton<MovieDetailCacheService>();
builder.Services.AddHostedService<MovieCacheService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MovieDetailCacheService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enable CORS for Vercel frontend and local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndLocalhost", policy =>
    {
        policy.WithOrigins(
            "https://webjet-fullstack-frontend.vercel.app", // Vercel frontend
            "http://localhost:5173" // Local development
        )
        .WithMethods("GET") // ✅ Only Allow GET Requests
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Enable CORS
app.UseCors("AllowVercelAndLocalhost");

// Enable Swagger for API documentation
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Webjet API V1");
});

// Enable authorization middleware
app.UseAuthorization();
app.MapControllers();

// Add health check endpoint
app.MapGet("/healthz", () => Results.Ok("Healthy"));

// Get PORT from environment variable (default: 5149 for local testing)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5149";

// Ensure correct binding for Render and local development
if (Environment.GetEnvironmentVariable("RENDER") == "true")
{
    // Render deployment: Bind to all network interfaces (necessary for Render)
    app.Urls.Add($"http://0.0.0.0:{port}");
}
else
{
    // Local development: Bind only to localhost
    app.Urls.Add($"http://localhost:{port}");
}

// Log application startup messages
Log.Information("Webjet API has started successfully.");
Log.Information("Running on port: {Port}", port);
Log.Information("Swagger UI available at: http://localhost:{Port}/swagger", port);

// Run the application
app.Run();
