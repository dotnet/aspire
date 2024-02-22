// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosDB("db");

var app = builder.Build();

app.MapGet("/", async (CosmosClient cosmosClient) =>
{
    var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync("db")).Database;
    var container = (await db.CreateContainerIfNotExistsAsync("entries", "/Id")).Container;

    // Add an entry to the database on each request.
    var newEntry = new Entry() { Id = Guid.NewGuid().ToString() };
    await container.CreateItemAsync(newEntry);

    var entries = new List<Entry>();
    var iterator = container.GetItemQueryIterator<Entry>(requestOptions: new QueryRequestOptions() { MaxItemCount = 5 });

    var batchCount = 0;
    while (iterator.HasMoreResults)
    {
        batchCount++;
        var batch = await iterator.ReadNextAsync();
        foreach (var entry in batch)
        {
            entries.Add(entry);
        }
    }

    return new
    {
        batchCount = batchCount,
        totalEntries = entries.Count,
        entries = entries
    };
});

app.Run();

public class Entry
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}
