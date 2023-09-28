// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient(
    "dapr",
    (provider, httpClient) =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();

        int daprHttpPort = configuration.GetValue<int>("DAPR_HTTP_PORT");

        httpClient.BaseAddress = new Uri($"http://localhost:{daprHttpPort}/v1.0/", UriKind.Absolute);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async ([FromServices] IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient("dapr");

    string serviceAppId = "service-b";

    var invocationUrl = new Uri($"invoke/{serviceAppId}/method/weatherforecast", UriKind.Relative);

    var forecast = await httpClient.GetFromJsonAsync<WeatherForecast[]>(invocationUrl);

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
