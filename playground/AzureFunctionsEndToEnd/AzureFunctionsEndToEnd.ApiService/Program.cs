using System.Security.Cryptography;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();
builder.AddAzureQueueClient("queue");
builder.AddAzureBlobClient("blob");
builder.AddAzureEventHubProducerClient("eventhubs", static settings => settings.EventHubName = "myhub");

var app = builder.Build();

app.MapGet("/publish/asq", async (QueueServiceClient client, CancellationToken cancellationToken) =>
{
    var queue = client.GetQueueClient("queue");
    await queue.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
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
    var container = client.GetBlobContainerClient("blobs");
    await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

    var entry = new { Id = Guid.NewGuid(), Text = RandomString(length) };
    var blob = container.GetBlobClient(entry.Id.ToString());

    await blob.UploadAsync(new BinaryData(entry));

    return Results.Ok("String uploaded to Azure Storage Blobs.");
});

app.MapGet("/publish/eventhubs", async (EventHubProducerClient client, CancellationToken cancellationToken, int length = 20) =>
{
    var data = new BinaryData(Encoding.UTF8.GetBytes(RandomString(length)));
    await client.SendAsync([new EventData(data)]);
    return Results.Ok("Message sent to Azure EventHubs.");
});

app.MapDefaultEndpoints();

app.Run();
