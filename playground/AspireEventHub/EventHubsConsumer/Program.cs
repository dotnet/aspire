using Azure.Identity;
using EventHubsConsumer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

Console.WriteLine("EventHub consumer/processor test");

bool useConsumer = Environment.GetEnvironmentVariable("USE_EVENTHUBCONSUMERCLIENT") == "yes";

if (useConsumer)
{
    builder.AddAzureEventHubConsumerClient("eventhubns",
        settings =>
        {
            settings.EventHubName = "hub";
            settings.Credential = new AzureCliCredential();
        });

    builder.Services.AddHostedService<Consumer>();
    Console.WriteLine("Starting EventHubConsumerClient...");
}
else
{
    // FIXME: this DI instance is never used as it's not reachable from the processor client configurator; we only use the connection name.
    builder.AddAzureBlobClient("checkpoints");

    builder.AddAzureEventProcessorClient("eventhubns",
        settings =>
        {
            settings.EventHubName = "hub";
            settings.BlobClientConnectionName = "checkpoints";
            settings.Credential = new AzureCliCredential();
        });
    builder.Services.AddHostedService<Processor>();
    Console.WriteLine("Starting EventProcessorClient...");
}

var host = builder.Build();

await host.RunAsync();
