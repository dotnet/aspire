using System.Reflection.Metadata;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AspirePlusFunctions.Functions;

public class MyCosmosDbTrigger(ILogger<MyCosmosDbTrigger> logger)
{
    [Function(nameof(MyCosmosDbTrigger))]
    public void Run([CosmosDBTrigger("mydatabase", "mycontainer", Connection = "cosmosdb")] IReadOnlyList<Document> input)
    {
        logger.LogInformation("C# cosmosdb trigger function processed: {Count} messages", input.Count);
    }
}
