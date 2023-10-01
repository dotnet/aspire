using OrderProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// When running for Development, don't fail at startup if the developer hasn't configured ServiceBus yet.
if (!builder.Environment.IsDevelopment() || builder.Configuration[AspireServiceBusExtensions.DefaultNamespaceConfigKey] is not null)
{
    builder.AddAzureServiceBus();
}

builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
