using EventHubsConsumer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

Console.WriteLine("EventHub consumer/processor test");

bool useConsumer = Environment.GetEnvironmentVariable("USE_EVENTHUBCONSUMERCLIENT") == "yes";

if (useConsumer)
{
    builder.AddAzureEventHubConsumerClient("eventhubOne");

    builder.Services.AddHostedService<Consumer>();
    Console.WriteLine("Starting EventHubConsumerClient...");
}
else
{
    // required for checkpointing our position in the event stream
    builder.AddAzureBlobServiceClient("checkpoints");

    builder.AddAzureEventProcessorClient("eventhubOne");

    builder.Services.AddHostedService<Processor>();
    Console.WriteLine("Starting EventProcessorClient...");
}

var host = builder.Build();

await host.RunAsync();
