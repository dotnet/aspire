using System.Security.Cryptography;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
#if !SKIP_UNSTABLE_EMULATORS
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
#endif
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddAzureQueueClient("queues");
builder.AddAzureBlobClient("blobs");
builder.AddAzureEventHubProducerClient("myhub");
#if !SKIP_UNSTABLE_EMULATORS
builder.AddAzureServiceBusClient("messaging");
builder.AddAzureCosmosClient("cosmosdb");
#endif

var app = builder.Build();

app.MapGet("/", async (HttpClient client) =>
{
    var stream = await client.GetStreamAsync("http://funcapp/api/injected-resources");
    return Results.Stream(stream, "application/json");
});

app.MapGet("/publish/asq", async (QueueServiceClient client, CancellationToken cancellationToken) =>
{
    var queue = client.GetQueueClient("myqueue1");

    var data = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello, World!"));
    await queue.SendMessageAsync(data, cancellationToken: cancellationToken);
    return Results.Ok("Message sent to Azure Storage Queue.");
});

static string RandomString(int length)
{
    const string chars = "abcdefghijklmnopqrstuvwxyz";
    return RandomNumberGenerator.GetString(chars, length);
}

app.MapGet("/publish/blob", async (BlobServiceClient client, CancellationToken cancellationToken, int length = 20) =>
{
    var container = client.GetBlobContainerClient("myblobcontainer");

    var entry = new { Id = Guid.NewGuid(), Text = RandomString(length) };
    var blob = container.GetBlobClient(entry.Id.ToString());

    await blob.UploadAsync(new BinaryData(entry));

    return Results.Ok($"String uploaded to Azure Storage Blobs {container.Uri}.");
});

app.MapGet("/publish/eventhubs", async (EventHubProducerClient client, CancellationToken cancellationToken, int length = 20) =>
{
    var data = new BinaryData(Encoding.UTF8.GetBytes(RandomString(length)));
    await client.SendAsync([new EventData(data)]);
    return Results.Ok("Message sent to Azure EventHubs.");
});

#if !SKIP_UNSTABLE_EMULATORS
app.MapGet("/publish/asb", async (ServiceBusClient client, CancellationToken cancellationToken, int length = 20) =>
{
    var sender = client.CreateSender("myqueue");
    var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(RandomString(length)));
    await sender.SendMessageAsync(message, cancellationToken);
    return Results.Ok("Message sent to Azure Service Bus.");
});

app.MapGet("/publish/cosmosdb", async (CosmosClient cosmosClient) =>
{
    var db = cosmosClient.GetDatabase("mydatabase");
    var container = db.GetContainer("mycontainer");

    var entry = new Entry { Id = Guid.NewGuid().ToString(), Text = RandomString(20) };
    await container.CreateItemAsync(entry);

    return Results.Ok("Document created in Azure Cosmos DB.");
});
#endif

app.MapDefaultEndpoints();

app.Run();

public class Entry
{
    [JsonProperty("id")]
    public required string Id { get; set; }

    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;
}
