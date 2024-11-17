using Milvus.Client;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddMilvusClient("milvus");

// Add services to the container.
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapDefaultEndpoints();

app.MapGet("/create", async (MilvusClient milvusClient, ILogger<Program> logger) =>
{
    string collectionName = "book";
    MilvusCollection collection = milvusClient.GetCollection(collectionName);

    //Check if this collection exists
    var hasCollection = await milvusClient.HasCollectionAsync(collectionName);

    if (hasCollection)
    {
        await collection.DropAsync();
        Console.WriteLine("Drop collection {0}", collectionName);
    }

    collection = await milvusClient.CreateCollectionAsync(
                collectionName,
                new[] {
                FieldSchema.Create<long>("book_id", isPrimaryKey:true),
                FieldSchema.Create<long>("word_count"),
                FieldSchema.CreateVarchar("book_name", 256),
                FieldSchema.CreateFloatVector("book_intro", 2)
                }
            );
    logger.LogInformation("Collection created: book");

    Random ran = new();
    List<long> bookIds = new();
    List<long> wordCounts = new();
    List<ReadOnlyMemory<float>> bookIntros = new();
    List<string> bookNames = new();
    for (long i = 0L; i < 2000; ++i)
    {
        bookIds.Add(i);
        wordCounts.Add(i + 10000);
        bookNames.Add($"Book Name {i}");

        float[] vector = new float[2];
        for (int k = 0; k < 2; ++k)
        {
            vector[k] = ran.Next();
        }
        bookIntros.Add(vector);
    }

    MutationResult result = await collection.InsertAsync(
        new FieldData[]
        {
        FieldData.Create("book_id", bookIds),
        FieldData.Create("word_count", wordCounts),
        FieldData.Create("book_name", bookNames),
        FieldData.CreateFloatVector("book_intro", bookIntros),
        });

    logger.LogInformation("Added vectors");

    // Check result
    logger.LogInformation("Insert status: {0},", result.ToString());

    // Create index
    await collection.CreateIndexAsync(
    "book_intro",
    //MilvusIndexType.IVF_FLAT,//Use MilvusIndexType.IVF_FLAT.
    IndexType.AutoIndex,//Use MilvusIndexType.AUTOINDEX when you are using zilliz cloud.
    SimilarityMetricType.L2);

    // Check index status
    IList<MilvusIndexInfo> indexInfos = await collection.DescribeIndexAsync("book_intro");

    foreach (var info in indexInfos)
    {
        logger.LogInformation("FieldName:{0}, IndexName:{1}, IndexId:{2}", info.FieldName, info.IndexName, info.IndexId);
    }

    logger.LogInformation("Index created");

    // Then load it
    await collection.LoadAsync();

    logger.LogInformation("Collection loaded");

    return Results.Ok("Collection created");
});

app.MapGet("/search", async (MilvusClient milvusClient, ILogger<Program> logger) =>
{
    MilvusCollection collection = milvusClient.GetCollection("book");

    // Query
    string expr = "book_id in [2,4,6,8]";

    QueryParameters queryParameters = new();
    queryParameters.OutputFields.Add("book_id");
    queryParameters.OutputFields.Add("book_name");
    queryParameters.OutputFields.Add("word_count");

    IReadOnlyList<FieldData> queryResult = await collection.QueryAsync(
        expr,
        queryParameters);

    return queryResult[2];
});

app.Run();
