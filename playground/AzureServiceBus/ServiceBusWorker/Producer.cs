// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Azure.Messaging.ServiceBus;

namespace ServiceBusWorker;

internal sealed class Producer(ServiceBusSender sender, ILogger<Producer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting producer...");

        var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (!cancellationToken.IsCancellationRequested)
        {
            await periodicTimer.WaitForNextTickAsync(cancellationToken);

            await sender.SendMessageAsync(new ServiceBusMessage($"Hello, World! It's {DateTime.Now} here."), cancellationToken);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping producer...");
        return Task.CompletedTask;
    }
}
