// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobClient("blobs")
       .AddKeyedAzureBlobContainerClient(blobContainerName: "test-container-1")
       .AddKeyedAzureBlobContainerClient(blobContainerName: "test-container-2");
builder.AddKeyedAzureBlobContainerClient("foocontainer");

builder.AddAzureQueueClient("queues");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", async ([FromKeyedServices("test-container-1")] BlobContainerClient keyedContainerClient1,
                       [FromKeyedServices("foocontainer")] BlobContainerClient keyedContainerClient2) =>
{
    var blobNames = new List<string>();
    var blobNameAndContent = Guid.NewGuid().ToString();

    await keyedContainerClient1.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));
    await keyedContainerClient2.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    await ReadBlobsAsync(keyedContainerClient1, blobNames);
    await ReadBlobsAsync(keyedContainerClient2, blobNames);

    return blobNames;
});
app.MapGet("/test", async (BlobServiceClient bsc, QueueServiceClient qsc, [FromKeyedServices("test-container-2")] BlobContainerClient keyedContainerClient1) =>
{
    var blobNames = new List<string>();
    var blobNameAndContent = Guid.NewGuid().ToString();

    await keyedContainerClient1.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var directContainerClient = bsc.GetBlobContainerClient(blobContainerName: "test-container-1");
    await directContainerClient.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    await ReadBlobsAsync(directContainerClient, blobNames);
    await ReadBlobsAsync(keyedContainerClient1, blobNames);

    var queue = qsc.GetQueueClient("myqueue");
    await queue.CreateIfNotExistsAsync();
    await queue.SendMessageAsync("Hello, world!");

    return blobNames;
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
