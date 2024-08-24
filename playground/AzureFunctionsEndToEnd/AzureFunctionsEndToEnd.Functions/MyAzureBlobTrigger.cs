using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureBlobTrigger
{
    private readonly ILogger<MyAzureBlobTrigger> _logger;

    public MyAzureBlobTrigger(ILogger<MyAzureBlobTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(MyAzureBlobTrigger))]
    [BlobOutput("test-files/{name}.txt")]
    public string Run([BlobTrigger("blobs/{name}")] string triggerString)
    {
        _logger.LogInformation($"C# blob trigger function invoked with {triggerString}...");
        return triggerString.ToUpper();
    }
}

