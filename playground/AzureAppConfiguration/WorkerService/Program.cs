using Microsoft.FeatureManagement;
using WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureAppConfigurationClient("appConfig");
builder.Services.AddHostedService<Worker>();
builder.Configuration.AddAzureAppConfiguration(
    "appconfig",
    configureOptions: options => {
        options.UseFeatureFlags();
        options.ConfigureRefresh(refresh =>
        {
            refresh.RegisterAll();
        });
        builder.Services.AddSingleton(options.GetRefresher());
    });
builder.Services.AddFeatureManagement();

var host = builder.Build();
host.Run();
