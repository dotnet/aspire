using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureBlobTrigger(ILogger<MyAzureBlobTrigger> logger)
{
    [Function(nameof(MyAzureBlobTrigger))]
    [BlobOutput("test-files/{name}.txt")]
    public string Run([BlobTrigger("blobs/{name}")] string triggerString)
    {
        logger.LogInformation("C# blob trigger function invoked with {message}...", triggerString);
        return triggerString.ToUpper();
    }
}

