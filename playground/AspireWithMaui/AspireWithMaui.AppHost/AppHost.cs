var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject("webapi", @"../AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj");

var publicDevTunnel = builder.AddDevTunnel("devtunnel-public")
    .WithAnonymousAccess() // All ports on this tunnel default to allowing anonymous access
    .WithReference(weatherApi.GetEndpoint("https"));

builder.AddMauiProject("mauiclient", @"../AspireWithMaui.MauiClient/AspireWithMaui.MauiClient.csproj")
    .WithWindows()
    .WithAndroid()
    .WithiOS()
    .WithMacCatalyst()
    .WithOtlpDevTunnel() // Setup a tunnel for the Open Telemetry communication, only needed for Android & iOS, but also works for desktop
    .WithReference(weatherApi, publicDevTunnel);

builder.Build().Run();
