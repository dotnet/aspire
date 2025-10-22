var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject("webapi", @"../AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj");

var mauiapp = builder.AddMauiProject("mauiapp", @"../AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj");

mauiapp.AddWindowsDevice()
    .WithReference(weatherApi);

builder.Build().Run();
