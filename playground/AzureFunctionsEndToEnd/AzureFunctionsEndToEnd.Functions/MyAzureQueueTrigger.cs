using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyAzureQueueTrigger(QueueClient queueClient, ILogger<MyAzureQueueTrigger> logger)
{
    [Function(nameof(MyAzureQueueTrigger))]
    public void Run([QueueTrigger("queue", Connection = "queue-queues")] QueueMessage message)
    {
        _ = queueClient.GetProperties();
        
        logger.LogInformation("C# Queue trigger function processed: {Text}", message.MessageText);
    }
}

