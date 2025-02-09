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

// Configure Redis to prefer local instance, fallback to remote if unavailable
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var localRedis = "localhost:6379,abortConnect=false";
    var remoteRedis = configuration["Redis:ConnectionString"];

    try
    {
        Log.Information("Trying to connect to local Redis: {RedisConfig}", localRedis);
        return ConnectionMultiplexer.Connect(localRedis);
    }
    catch
    {
        if (!string.IsNullOrEmpty(remoteRedis))
        {
            Log.Warning("Local Redis connection failed. Trying remote Redis: {RedisConfig}", remoteRedis);
            return ConnectionMultiplexer.Connect(remoteRedis);
        }
        else
        {
            throw new Exception("No available Redis connection. Ensure Redis is running.");
        }
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
