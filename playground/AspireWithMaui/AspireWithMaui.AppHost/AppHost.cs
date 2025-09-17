var builder = DistributedApplication.CreateBuilder(args);

// Add the Weather API service with external HTTP endpoints for mobile device access
var weatherApi = builder.AddProject("weatherapi", @"..\AspireWithMaui.WeatherApi\AspireWithMaui.WeatherApi.csproj")
    .WithExternalHttpEndpoints();

// Add MAUI client for Windows platform
builder.AddMauiProject("mauiclient-windows", @"..\AspireWithMaui.MauiClient\AspireWithMaui.MauiClient.csproj")
    .WithReference(weatherApi)
    .WithWindows();

builder.Build().Run();
