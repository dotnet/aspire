// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddMongoDBClient("db");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/", async (IMongoClient mongoClient) =>
{
    const string collectionName = "entries";

    var db = mongoClient.GetDatabase("db");
    await db.CreateCollectionAsync(collectionName);

    // Add an entry to the database on each request.
    var newEntry = new Entry();
    await db.GetCollection<Entry>(collectionName).InsertOneAsync(newEntry);

    var items = await db.GetCollection<Entry>(collectionName).FindAsync(_ => true);

    return items.ToListAsync();
});

app.Run();

public class Entry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
}
