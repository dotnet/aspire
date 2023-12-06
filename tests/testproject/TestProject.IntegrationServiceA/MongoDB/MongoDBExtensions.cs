// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MongoDB.Driver;

public static class MongoDBExtensions
{
    public static void MapMongoMovieApi(this WebApplication app)
    {
        // Resolving IMongoDatabase creates the database automatically from the connection string property

        var database = app.Services.GetRequiredService<IMongoDatabase>();
        database.CreateCollection("movies");
        var collection = database.GetCollection<Movie>("movies");
        collection.InsertOne(new Movie(1, "Rocky I"));
        collection.InsertOne(new Movie(2, "Rocky II"));

        app.MapGet("/mongodb/databases", GetDatabaseNamesAsync);

        app.MapGet("/mongodb/movies", GetMoviesAsync);
    }
    private static async Task<List<string>> GetDatabaseNamesAsync(IMongoClient client)
    {
        var databaseNames = new List<string>();

        await client.ListDatabaseNames().ForEachAsync(databaseNames.Add);

        return databaseNames;
    }

    private static async Task<List<string>> GetMoviesAsync(IMongoDatabase db)
    {
        var moviesCollection = db.GetCollection<Movie>("movies");

        return (await moviesCollection.Find(x => true).ToListAsync()).Select(x => x.Name).ToList();
    }
}
