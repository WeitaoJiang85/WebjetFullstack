using DotNetEnv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using WebjetAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Load .env file for environment variables
if (File.Exists("/etc/secrets/.env"))
{
    Env.Load("/etc/secrets/.env");
}
else
{
    Env.Load();
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
var apiToken = Env.GetString("WEBJET_API_TOKEN") ?? throw new Exception("API Token is missing from .env file");

// Load Redis connection string from .env file
var redisConnection = Env.GetString("REDIS_CONNECTION_STRING") ?? throw new Exception("Redis connection string is missing from .env file");

try
{
    Log.Information("Connecting to Redis at {RedisConfig}", redisConnection);
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnection));
}
catch (Exception ex)
{
    Log.Error("Failed to connect to Redis: {ErrorMessage}", ex.Message);
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null);
}

// Register services
builder.Services.AddSingleton<MovieDetailCacheService>();
builder.Services.AddHostedService<MovieCacheService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MovieDetailCacheService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS configuration: Allow AKS frontend & local development
var allowedOrigins = new[] { "http://4.254.122.98", "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(allowedOrigins) 
                  .WithMethods("GET", "POST", "OPTIONS") 
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// Enable CORS globally
app.UseCors("AllowFrontend");

// Handle OPTIONS requests to prevent 405 error
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 204;
        return;
    }
    await next();
});

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

// Ensure correct binding for Docker, Kubernetes, or local development
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var isDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
var isKubernetes = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST") != null;

if (isDocker || isKubernetes)
{
    app.Urls.Add($"http://0.0.0.0:{port}");
}
else
{
    app.Urls.Add($"http://localhost:{port}");
}

// Log application startup messages
Log.Information("Webjet API has started successfully.");
Log.Information("Running on port: {Port}", port);
Log.Information("Swagger UI available at: http://localhost:{Port}/swagger", port);

// Run the application
app.Run();
