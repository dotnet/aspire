// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr;
using Dapr.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDaprClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapGet("/weatherforecast", async (DaprClient client) =>
{
    var cachedForecasts = await client.GetStateAsync<CachedWeatherForecast>("statestore", "cache");

    if (cachedForecasts is not null && cachedForecasts.CachedAt > DateTimeOffset.UtcNow.AddMinutes(-1))
    {
        return cachedForecasts.Forecasts;
    }

    var forecasts = await client.InvokeMethodAsync<WeatherForecast[]>(HttpMethod.Get, "serviceb", "weatherforecast");

    await client.SaveStateAsync("statestore", "cache", new CachedWeatherForecast(forecasts, DateTimeOffset.UtcNow));

    return forecasts;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/subscriptions/weather", [Topic("pubsub", "weather")] (ILogger<Program> logger, WeatherForecastMessage message) =>
{
    logger.LogInformation("Weather forecast message received: {Message}", message.Message);
});

app.MapDefaultEndpoints();

app.Run();

internal sealed record WeatherForecastMessage(string Message);

internal sealed record CachedWeatherForecast(WeatherForecast[] Forecasts, DateTimeOffset CachedAt);

internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
