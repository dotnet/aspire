// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Dapr.Client;
using Test;

var builder = Host.CreateApplicationBuilder(args);

var dapr = new DaprClientBuilder()
    .Build();

builder.Services.AddSingleton<DaprClient>(dapr);

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

await app.RunAsync();

Console.WriteLine("Goodbye, World!");

namespace Test
{
    public sealed class Worker(ILogger<Worker> logger, DaprClient dapr) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await dapr.WaitForSidecarAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var state = await dapr.GetStateAsync<string?>(
                    "statestore", "cache", cancellationToken: stoppingToken);

                logger.LogInformation("State: {0}", state ?? "<null>");

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
