// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureStorageEmulatorFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnAzureStorageEmulatorForBlobsBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var storage = builder.AddAzureStorage("resource")
                              .RunAsEmulator()
                              .WithHealthCheck("blocking_check");

        var blobs = storage.AddBlobs("blobs");
        var queues = storage.AddQueues("queues");
        var tables = storage.AddTables("tables");

        var dependentResource = builder.AddContainer("nginx", "mcr.microsoft.com/cbl-mariner/base/nginx", "1.22")
                                       .WaitFor(blobs)
                                       .WaitFor(queues)
                                       .WaitFor(tables);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(storage.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(blobs.Resource.Name, cts.Token);
        await rns.WaitForResourceHealthyAsync(queues.Resource.Name, cts.Token);
        await rns.WaitForResourceHealthyAsync(tables.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyAzureStorageEmulatorResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(testOutputHelper);
        var storage = builder.AddAzureStorage("storage").RunAsEmulator().AddBlobs("BlobConnection");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration["ConnectionStrings:BlobConnection"] = await storage.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        hb.AddAzureBlobClient("BlobConnection");

        using var host = hb.Build();
        await host.StartAsync();

        var serviceClient = host.Services.GetRequiredService<BlobServiceClient>();
        var blobContainer = (await serviceClient.CreateBlobContainerAsync("container")).Value;
        var blobClient = blobContainer.GetBlobClient("testKey");

        await blobClient.UploadAsync(BinaryData.FromString("testValue"));

        var downloadResult = (await blobClient.DownloadContentAsync()).Value;
        Assert.Equal("testValue", downloadResult.Content.ToString());
    }
}
