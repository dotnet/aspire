// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAzureBlobService("blobs");
builder.AddSqlServerDbContext<SqlContext>("sqldb");
builder.AddAzureKeyVaultSecrets("mykv");

var app = builder.Build();

app.MapGet("/", async (BlobServiceClient bsc, SqlContext context, SecretClient sc) =>
{
    return new
    {
        secretChecked = await TestSecretAsync(sc),
        blobFiles = await TestBlobStorageAsync(bsc),
        sqlRows = await TestSqlServerAsync(context)
    };
});

app.Run();

static async Task<bool> TestSecretAsync(SecretClient secretClient)
{
    KeyVaultSecret s = await secretClient.GetSecretAsync("mysecret");
    return s.Value == "open sesame";
}

static async Task<List<string>> TestBlobStorageAsync(BlobServiceClient bsc)
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
}

static async Task<List<Entry>> TestSqlServerAsync(SqlContext context)
{
    await context.Database.EnsureCreatedAsync();

    var entry = new Entry();
    await context.Entries.AddAsync(entry);
    await context.SaveChangesAsync();

    var entries = await context.Entries.ToListAsync();
    return entries;
}

public class SqlContext(DbContextOptions<SqlContext> options) : DbContext(options)
{
    public DbSet<Entry> Entries { get; set; }
}
public class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
