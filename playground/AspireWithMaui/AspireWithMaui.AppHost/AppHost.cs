var builder = DistributedApplication.CreateBuilder(args);

// Add the Weather API service with external HTTP endpoints for mobile device access
builder.AddProject("AspireWithMaui-WeatherApi", @"..\AspireWithMaui.WeatherApi\AspireWithMaui.WeatherApi.csproj")
    .WithExternalHttpEndpoints();

builder.AddMauiProject("mauiclient", @"..\AspireWithMaui.MauiClient\AspireWithMaui.MauiClient.csproj")
    .WithWindows()
    .WithAndroid();

builder.Build().Run();
