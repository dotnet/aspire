// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobClient("blobs");
builder.AddKeyedAzureBlobContainerClient("foocontainer");

builder.AddAzureQueueClient("queues");
builder.AddKeyedAzureQueue("myqueue");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", (HttpContext context) =>
{
    var request = context.Request;
    var scheme = request.Scheme;
    var host = request.Host;

    var endpointDataSource = context.RequestServices.GetRequiredService<EndpointDataSource>();
    var urls = endpointDataSource.Endpoints
        .OfType<RouteEndpoint>()
        .Select(e => $"{scheme}://{host}{e.RoutePattern.RawText}");

    var html = "<html><body><ul>" +
               string.Join("", urls.Select(url => $"<li><a href=\"{url}\">{url}</a></li>")) +
               "</ul></body></html>";

    context.Response.ContentType = "text/html";
    return context.Response.WriteAsync(html);
});

app.MapGet("/blobs", async (BlobServiceClient bsc, [FromKeyedServices("foocontainer")] BlobContainerClient bcc) =>
{
    var blobNames = new List<string>();
    var blobNameAndContent = Guid.NewGuid().ToString();

    await bcc.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var directContainerClient = bsc.GetBlobContainerClient(blobContainerName: "test-container-1");
    await directContainerClient.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    await ReadBlobsAsync(directContainerClient, blobNames);
    await ReadBlobsAsync(bcc, blobNames);

    return blobNames;
});

app.MapGet("/queues", async (QueueServiceClient qsc, [FromKeyedServices("myqueue")] QueueClient qc) =>
{
    const string text = "Hello, World!";
    List<string> messages = [$"Sent: {text}"];

    var queue = qsc.GetQueueClient("my-queue");
    await queue.SendMessageAsync(text);

    var msg = await qc.ReceiveMessageAsync();
    messages.Add($"Received: {msg.Value.Body}");

    return messages;
});

app.Run();

static async Task ReadBlobsAsync(BlobContainerClient containerClient, List<string> output)
{
    output.Add(containerClient.Uri.ToString());
    var blobs = containerClient.GetBlobsAsync();
    await foreach (var blob in blobs)
    {
        output.Add(blob.Name);
    }
}
