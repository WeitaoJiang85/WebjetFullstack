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

// Configure Serilog for logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/webjetapi-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add HttpClientFactory for handling external API requests
builder.Services.AddHttpClient();

// Configure Redis to use Railway Redis from appsettings.json
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var remoteRedis = configuration["Redis:ConnectionString"];

    if (string.IsNullOrEmpty(remoteRedis))
    {
        throw new Exception("Redis connection string is missing in configuration.");
    }

    try
    {
        Log.Information("Connecting to Railway Redis: {RedisConfig}", remoteRedis);
        return ConnectionMultiplexer.Connect(remoteRedis);
    }
    catch (Exception ex)
    {
        throw new Exception($"Failed to connect to Redis: {ex.Message}");
    }
});

// Register services
builder.Services.AddSingleton<MovieDetailCacheService>();
builder.Services.AddHostedService<MovieCacheService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MovieDetailCacheService>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Log application start message
Log.Information("Webjet API has started successfully.");
Log.Information("Swagger UI available at: http://localhost:5149/swagger");

app.Run();
