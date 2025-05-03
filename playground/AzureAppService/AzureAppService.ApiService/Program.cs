// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddCosmosDbContext<TestCosmosContext>("account", "db");
builder.AddAzureBlobClient("blobs");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () =>
{
    return Results.Content("""
    <html>
        <body>
            <ul>
                <li><a href="/blobs">Blobs</a></li>
                <li><a href="/cosmos">Cosmos</a></li>
            </ul>
        </body>
    </html>
    """,
    "text/html");
});

app.MapGet("/blobs", async (BlobServiceClient bsc) =>
{
    var container = bsc.GetBlobContainerClient("mycontainer");
    await container.CreateIfNotExistsAsync();

    var blobNameAndContent = Guid.NewGuid().ToString();
    await container.UploadBlobAsync(blobNameAndContent, new BinaryData(blobNameAndContent));

    var blobs = container.GetBlobsAsync();

    var blobNames = new List<string>();

    await foreach (var blob in blobs)
    {
        blobNames.Add(blob.Name);
    }

    return blobNames;
});

app.MapGet("/cosmos", async (TestCosmosContext context) =>
{
    await context.Database.EnsureCreatedAsync();

    context.Entries.Add(new EntityFrameworkEntry());
    await context.SaveChangesAsync();

    return await context.Entries.ToListAsync();
});

app.Run();

public class Entry
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class TestCosmosContext(DbContextOptions<TestCosmosContext> options) : DbContext(options)
{
    public DbSet<EntityFrameworkEntry> Entries { get; set; }
}

public class EntityFrameworkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}

