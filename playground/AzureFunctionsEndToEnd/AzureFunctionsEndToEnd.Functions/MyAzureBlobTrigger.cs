using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureBlobTrigger(ILogger<MyAzureBlobTrigger> logger)
{
    [Function(nameof(MyAzureBlobTrigger))]
    [BlobOutput("test-files/{name}.txt", Connection = "blob")]
    public string Run([BlobTrigger("blobs/{name}", Connection = "blob")] string triggerString)
    {
        logger.LogInformation("C# blob trigger function invoked with {message}...", triggerString);
        return triggerString.ToUpper();
    }
}

