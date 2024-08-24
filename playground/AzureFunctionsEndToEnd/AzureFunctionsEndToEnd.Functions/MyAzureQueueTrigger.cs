using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureQueueTrigger
{
    private readonly ILogger<MyAzureQueueTrigger> _logger;

    public MyAzureQueueTrigger(ILogger<MyAzureQueueTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(MyAzureQueueTrigger))]
    public void Run([QueueTrigger("queue")] QueueMessage message)
    {
        _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");
    }
}

