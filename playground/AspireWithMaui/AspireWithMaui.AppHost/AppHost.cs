var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject("webapi", @"../AspireWithMaui.WeatherApi/AspireWithMaui.WeatherApi.csproj");

builder.AddDevTunnel("devtunnel-public")
    .WithAnonymousAccess() // All ports on this tunnel default to allowing anonymous access
    .WithReference(weatherApi.GetEndpoint("https"));

builder.Build().Run();
