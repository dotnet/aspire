using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureBlobTrigger(BlobContainerClient containerClient, ILogger<MyAzureBlobTrigger> logger)
{
    [Function(nameof(MyAzureBlobTrigger))]
    [BlobOutput("test-files/{name}.txt", Connection = "blob-blobs")]
    public async Task<string> RunAsync([BlobTrigger("myblobcontainer/{name}", Connection = "blob-blobs")] string triggerString, FunctionContext context)
    {
        var blobName = (string)context.BindingContext.BindingData["name"]!;
        _ = await containerClient.GetAccountInfoAsync();

        logger.LogInformation("C# blob trigger function invoked for 'myblobcontainer/{source}' with {message}...", blobName, triggerString);
        return triggerString.ToUpper();
    }
}
