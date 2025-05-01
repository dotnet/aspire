// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobClient("blobs");
//builder.AddAzureBlobContainerClient("foocontainer");
builder.AddKeyedAzureBlobContainerClient("mycontainer2");
builder.AddKeyedAzureBlobContainerClient("foocontainer");

builder.AddAzureQueueClient("queues");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", async ([FromKeyedServices("mycontainer2")] BlobContainerClient keyedContainerClinet1,
                       [FromKeyedServices("foocontainer")] BlobContainerClient keyedContainerClinet2) =>
{
    var blobNames = new List<string>();
    var blobNameAndContent = Guid.NewGuid().ToString();

    await keyedContainerClinet1.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));
    await keyedContainerClinet2.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var blobs = keyedContainerClinet1.GetBlobsAsync();
    blobNames.Add(keyedContainerClinet1.Uri.ToString());
    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    blobs = keyedContainerClinet2.GetBlobsAsync();
    blobNames.Add(keyedContainerClinet2.Uri.ToString());
    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    return blobNames;
});
app.MapGet("/test", async (BlobServiceClient bsc, QueueServiceClient qsc, BlobContainerClient fooContainerClinet) =>
{
    var blobNameAndContent = Guid.NewGuid().ToString();

    var container = bsc.GetBlobContainerClient(blobContainerName: "test-container-1");
    await container.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var blobs = container.GetBlobsAsync();

    var blobNames = new List<string>();

    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    var queue = qsc.GetQueueClient("myqueue");
    await queue.CreateIfNotExistsAsync();
    await queue.SendMessageAsync("Hello, world!");

    return blobNames;
});

app.Run();
