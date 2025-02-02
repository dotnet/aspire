#if !SKIP_UNSTABLE_EMULATORS
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureFunctionsEndToEnd.Functions;

public class MyServiceBusTrigger(ILogger<MyServiceBusTrigger> logger)
{
    [Function(nameof(MyServiceBusTrigger))]
    public void Run([ServiceBusTrigger("myqueue", Connection = "messaging")] ServiceBusReceivedMessage message)
    {
        logger.LogInformation("Message ID: {id}", message.MessageId);
        logger.LogInformation("Message Body: {body}", message.Body);
        logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);
    }
}
#endif
