// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;
public class AzureSignalREmulatorFunctionalTest(ITestOutputHelper testOutputHelper)
{

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnAzureSignalREmulatorBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var signalR = builder.AddAzureSignalR("resource")
                            .RunAsEmulator()
                            .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
            .WaitFor(signalR);
        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(signalR.Resource.Name, KnownResourceStates.Running, cts.Token);
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(signalR.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyAzureSignalREmulatorResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var signalR = builder
            .AddAzureSignalR("signalR")
            .RunAsEmulator();

        using var app = builder.Build();
        await app.StartAsync();

        var connectionString = await signalR.Resource.ConnectionStringExpression.GetValueAsync(default);
        var serviceManager = new ServiceManagerBuilder()
            .WithOptions(option => { option.ConnectionString = connectionString; })
            .BuildServiceManager();
        Assert.True(await serviceManager.IsServiceHealthy(default));

        // Get negotiate URL to init a signalR connection
        var serviceHubContext = await serviceManager.CreateHubContextAsync("hub1", default);
        var negotiationResponse = await serviceHubContext.NegotiateAsync(new() { UserId = "testId" } );
        var connection = new HubConnectionBuilder().WithUrl(negotiationResponse.Url ?? "", option =>
        {
            option.AccessTokenProvider = () => Task.FromResult(negotiationResponse.AccessToken);
        }).Build();
        var messageTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<string>("broadcast", message =>
        {
            messageTcs.TrySetResult(message);
        });
        await connection.StartAsync();

        // Broadcast message to all clients
        var sentMessage = "Hello, World!";
        await serviceHubContext.Clients.All.SendAsync("broadcast", sentMessage);

        // Verify that received message is the same as sent message
        Assert.Equal(sentMessage, await messageTcs.Task);
        await app.StopAsync();
    }
}
