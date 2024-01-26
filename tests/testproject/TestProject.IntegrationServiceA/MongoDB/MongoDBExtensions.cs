// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MongoDB.Driver;

public static class MongoDBExtensions
{
    public static void MapMongoDBApi(this WebApplication app)
    {
        app.MapGet("/mongodb/verify", VerifyMongoDBAsync);
    }

    private static async Task<IResult> VerifyMongoDBAsync(IMongoDatabase db)
    {
        try
        {
            // Use a random collection to make the test idempotent

            var collectionName = Guid.NewGuid().ToString("N");
            db.CreateCollection(collectionName);
            var moviesCollection = db.GetCollection<Movie>(collectionName);
            await moviesCollection.InsertOneAsync(new Movie(1, "Rocky I"));
            await moviesCollection.InsertOneAsync(new Movie(2, "Rocky II"));

            var moviesCount = (await moviesCollection.Find(x => true).ToListAsync()).Count;

            return moviesCount == 2 ? Results.Ok("Success!") : Results.Problem("Failed");
        }
        catch (Exception e)
        {
            return Results.Problem(e.ToString());
        }

        
            
    }
}
