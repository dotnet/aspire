var builder = DistributedApplication.CreateBuilder(args);

// Add the Weather API service with external HTTP endpoints for mobile device access
builder.AddProject("AspireWithMaui-WeatherApi", @"..\AspireWithMaui.WeatherApi\AspireWithMaui.WeatherApi.csproj")
    .WithExternalHttpEndpoints();

// TODO: Add MAUI client - currently not supported
// We would like to do something like:
// builder.AddMauiProject<Projects.AspireWithMaui_MauiClient>("mauiclient")
//    .WithReference(weatherApi);
//
// But this functionality doesn't exist yet. The MAUI app needs to be run manually
// and configured to use service discovery to connect to the weather API.

builder.Build().Run();
