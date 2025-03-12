// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//using Microsoft.Azure.SignalR.Management;

using Microsoft.Azure.SignalR.Management;

namespace SignalRServerlessWeb;

public class BackgroundWorker(ServiceHubContext hubContext) : BackgroundService
{
    private int _count;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested) {
            await hubContext.Clients.All.SendCoreAsync("newMessage", [$"Current count is: {_count++}"], stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
