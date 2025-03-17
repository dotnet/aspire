using Aspire.Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.FeatureManagement;
using WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();
builder.Configuration.AddAzureAppConfiguration(
    "appconfig",
    configureSettings: settings => settings.Credential = new AzureCliCredential(),
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
