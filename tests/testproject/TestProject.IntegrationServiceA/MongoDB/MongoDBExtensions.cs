// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MongoDB.Driver;

public static class MongoDBExtensions
{
    public static void MapMongoMovieApi(this WebApplication app)
    {
        app.MapGet("/mongodb/databases", GetDatabaseNamesAsync);

        app.MapGet("/mongodb/movies", GetMoviesAsync);
    }

    private static async Task<List<string>> GetDatabaseNamesAsync(IMongoClient client, IMongoDatabase db)
    {
        // Ensure the database is created
        var randomCollection = db.GetCollection<Movie>("random");
        await randomCollection.DeleteManyAsync(x => true);
        randomCollection.InsertOne(new Movie(1, "123"));

        var databaseNames = new List<string>();

        await client.ListDatabaseNames().ForEachAsync(databaseNames.Add);

        return databaseNames;
    }

    private static async Task<List<string>> GetMoviesAsync(IMongoDatabase db)
    {
        db.CreateCollection("movies");
        var moviesCollection = db.GetCollection<Movie>("movies");
        await moviesCollection.DeleteManyAsync(x => true);
        await moviesCollection.InsertOneAsync(new Movie(1, "Rocky I"));
        await moviesCollection.InsertOneAsync(new Movie(2, "Rocky II"));

        return (await moviesCollection.Find(x => true).ToListAsync()).Select(x => x.Name).ToList();
    }
}
