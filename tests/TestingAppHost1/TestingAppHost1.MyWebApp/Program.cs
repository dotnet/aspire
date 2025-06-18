// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using TestingAppHost1.MyWebApp;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

var app = builder.Build();

builder.Services.AddDbContextPool<MyAppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("mainDb");
    options.UseNpgsql(connectionString);
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
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

app.MapGet("/get-launch-profile-var", () =>
{
    return app.Configuration["LAUNCH_PROFILE_VAR"];
}).WithName("GetLaunchProfileVar");

app.MapGet("/get-app-host-arg", () =>
{
    return app.Configuration["APP_HOST_ARG"];
}).WithName("GetAppHostArg");

app.MapGet("/get-launch-profile-var-from-app-host", () =>
{
    return app.Configuration["LAUNCH_PROFILE_VAR_FROM_APP_HOST"];
}).WithName("GetLaunchProfileVarFromAppHost");

app.Run();

sealed record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
