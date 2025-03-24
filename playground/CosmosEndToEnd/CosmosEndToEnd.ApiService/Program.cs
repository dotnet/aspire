// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureCosmosDatabase("db")
    .AddKeyedContainer("entries")
    .AddKeyedContainer("users");
builder.AddCosmosDbContext<TestCosmosContext>("db", configureDbContextOptions =>
{
    configureDbContextOptions.RequestTimeout = TimeSpan.FromSeconds(120);
});

var app = builder.Build();

app.MapDefaultEndpoints();

static async Task<object> AddAndGetStatus<T>(Container container, T newEntry)
{
    await container.CreateItemAsync(newEntry);

    var entries = new List<T>();
    var iterator = container.GetItemQueryIterator<T>(requestOptions: new QueryRequestOptions() { MaxItemCount = 5 });

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
        batchCount,
        totalEntries = entries.Count,
        entries
    };
}

app.MapGet("/", async ([FromKeyedServices("entries")] Container container) =>
{
    var newEntry = new Entry() { Id = Guid.NewGuid().ToString() };
    return await AddAndGetStatus(container, newEntry);
});

app.MapGet("/users", async ([FromKeyedServices("users")] Container container) =>
{
    var newEntry = new User() { Id = $"user-{Guid.NewGuid()}" };
    return await AddAndGetStatus(container, newEntry);
});

app.MapGet("/ef", async (TestCosmosContext context) =>
{
    await context.Database.EnsureCreatedAsync();

    context.Entries.Add(new EntityFrameworkEntry());
    await context.SaveChangesAsync();

    return await context.Entries.ToListAsync();
});

app.Run();

public class User
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class Entry
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class TestCosmosContext(DbContextOptions<TestCosmosContext> options) : DbContext(options)
{
    public DbSet<EntityFrameworkEntry> Entries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EntityFrameworkEntry>()
            .HasPartitionKey(e => e.Id);
    }
}

public class EntityFrameworkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
