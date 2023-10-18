using OrderProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// When running for Development, don't fail at startup if the developer hasn't configured ServiceBus yet.
if (!builder.Environment.IsDevelopment() || builder.Configuration.GetConnectionString("messaging") is not null)
{
    builder.AddAzureServiceBus("messaging");
}

builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
