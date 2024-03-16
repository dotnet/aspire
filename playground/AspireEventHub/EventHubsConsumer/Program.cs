using EventHubsConsumer;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

char[] cmds = ['c', 'p', (char)0x27];
char key;

do
{
    Console.Write("Consumer mode: EventHub[C]onsumerClient or Event[P]rocessorClient? ");
    key = Console.ReadKey().KeyChar;

    if (!cmds.Contains(key))
    {
        Console.WriteLine("Invalid mode. Please enter 'C' or 'P', or CTRL+C to exit.");
    }
    else if (key == 'c')
    {
        builder.AddAzureEventHubConsumerClient("eventhubns", settings =>
        {
            settings.EventHubName = "hub";
        });
        builder.Services.AddHostedService<Consumer>();
    }
    else if (key == 'p')
    {
        builder.AddAzureBlobClient("checkpoints");
        builder.AddAzureEventHubProcessorClient("eventhubns",
            settings =>
            {
                settings.EventHubName = "hub";
                settings.BlobClientConnectionString = "checkpoints";
            });

        builder.Services.AddHostedService<Processor>();
    }
} while (key != 0x27);

var host = builder.Build();

await host.RunAsync();
