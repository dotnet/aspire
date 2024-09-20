// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Meilisearch;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddMeilisearchClient("meilisearch");

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/search", async (MeilisearchClient meilisearch) =>
{
    var index = meilisearch.Index("movies");

    var result = await index.SearchAsync<Movie>(
     "car",
     new SearchQuery
     {
         AttributesToHighlight = ["title"],
     });

    return result;
});

app.MapGet("/create", async (MeilisearchClient meilisearch) =>
{
    // An index is where the documents are stored.
    var index = meilisearch.Index("movies");
    var documents = new Movie[] {
                new() { Id = "1", Title = "Carol", Genres = ["Romance", "Drama"]  },
                new() { Id = "2", Title = "Wonder Woman", Genres = ["Action", "Adventure"] },
                new() { Id = "3", Title = "Life of Pi", Genres = ["Adventure", "Drama"] },
                new() { Id = "4", Title = "Mad Max: Fury Road", Genres = ["Adventure", "Science Fiction"] },
                new() { Id = "5", Title = "Moana", Genres = ["Fantasy", "Action"] },
                new() { Id = "6", Title = "Philadelphia", Genres = ["Drama"] }
            };

    // If the index 'movies' does not exist, Meilisearch creates it when you first add the documents.
    var task = await index.AddDocumentsAsync<Movie>(documents);
    return task;
});

app.Run();
