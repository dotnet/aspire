using Qdrant.Client;
using Qdrant.Client.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

var connectionString = builder.Configuration.GetConnectionString("qdrant") ?? "http://localhost:6334";
var client = new QdrantClient(new Uri(connectionString), builder.Configuration["Parameters:ApiKey"]);
var random = new Random();

app.MapGet("/create", async () =>
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

app.MapGet("/search", async () =>
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
