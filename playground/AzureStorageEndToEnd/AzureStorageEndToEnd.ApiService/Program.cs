// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobClient("blobs");
builder.AddAzureQueueClient("queues");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (BlobServiceClient bsc, QueueServiceClient qsc) =>
{
    var container = bsc.GetBlobContainerClient("mycontainer");

    var blobNameAndContent = Guid.NewGuid().ToString();
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
