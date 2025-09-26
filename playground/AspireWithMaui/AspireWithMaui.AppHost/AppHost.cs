var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject("webapi", @"../AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj");

var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
    .WithAnonymousAccess() // All ports on this tunnel default to allowing anonymous access
    .WithReference(weatherApi.GetEndpoint("https"));

builder.AddMauiProject("mauiclient", @"../AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj")
    .WithWindows()
    .WithAndroid()
    .WithIOS()
    .WithOtlpDevTunnel()
    .WithReference(weatherApi, publicDevTunnel); // for service discovery of other services if needed

builder.Build().Run();
