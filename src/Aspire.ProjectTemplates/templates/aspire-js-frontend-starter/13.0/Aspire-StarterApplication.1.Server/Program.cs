#if (UseRedisCache)
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
#endif

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
#if (UseRedisCache)
builder.AddRedisDistributedCache("cache");
#endif

// Add services to the container.
builder.Services.AddProblemDetails();

#if (Framework != 'net8.0')
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

#endif
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

#if (Framework != 'net8.0')
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

#endif
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

var api = app.MapGroup("/api");
#if (UseRedisCache)
api.MapGet("weatherforecast", async (IDistributedCache cache) =>
{
    var cachedForecast = await cache.GetAsync("forecast");
    if (cachedForecast is null)
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        await cache.SetAsync("forecast", JsonSerializer.SerializeToUtf8Bytes(forecast), new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5) });
        return forecast;
    }

    return JsonSerializer.Deserialize<WeatherForecast[]>(cachedForecast);
})
.WithName("GetWeatherForecast");
#else
api.MapGet("weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
#endif

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
