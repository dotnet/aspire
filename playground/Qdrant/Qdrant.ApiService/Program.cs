using Qdrant.Client;
using Qdrant.Client.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.AddKeyedQdrantClient("qdrant");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var random = new Random();

app.MapGet("/create", async ([FromKeyedServices("qdrant")] QdrantClient client) =>
{
    await client.CreateCollectionAsync("my_collection", new VectorParams { Size = 100, Distance = Distance.Cosine });

    // generate some vectors
    var points = Enumerable.Range(1, 100).Select(i => new PointStruct
    {
        Id = (ulong)i,
        Vectors = Enumerable.Range(1, 100).Select(_ => (float)random.NextDouble()).ToArray(),
        Payload =
  {
    ["color"] = "red",
    ["rand_number"] = i % 10
  }
    }).ToList();

    var updateResult = await client.UpsertAsync("my_collection", points);

    return updateResult.Status;
});

app.MapGet("/search", async ([FromKeyedServices("qdrant")] QdrantClient client) =>
{
    var queryVector = Enumerable.Range(1, 100).Select(_ => (float)random.NextDouble()).ToArray();

    // return the 5 closest points
    var points = await client.SearchAsync(
      "my_collection",
      queryVector,
      limit: 5);

    return points;
});

app.MapDefaultEndpoints();

app.Run();
