// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyHttpTrigger(
    ILogger<MyHttpTrigger> logger,
    ServiceBusClient serviceBusClient,
    EventHubProducerClient eventHubProducerClient,
    QueueServiceClient queueServiceClient,
    BlobServiceClient blobServiceClient)
{
    [Function("injected-resources")]
    public IResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Aspire-injected ServiceBusClient namespace: {serviceBusClient.FullyQualifiedNamespace}");
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Aspire-injected EventHubProducerClient namespace: {eventHubProducerClient.FullyQualifiedNamespace}");
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Aspire-injected QueueServiceClient URI: {queueServiceClient.Uri}");
        stringBuilder.AppendLine(CultureInfo.InvariantCulture, $"Aspire-injected BlobServiceClient URI: {blobServiceClient.Uri}");
        return Results.Text(stringBuilder.ToString());
    }
}
