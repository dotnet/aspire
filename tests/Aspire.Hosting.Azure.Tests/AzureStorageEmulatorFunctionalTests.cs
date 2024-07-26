// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Redis.Tests;

public class AzureStorageEmulatorFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyAzureStorageEmulatorResource()
    {
        using var builder = CreateDistributedApplicationBuilder();
        var storage = builder.AddAzureStorage("storage").RunAsEmulator().AddBlobs("BlobConnection");

        using var app = builder.Build();
        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();
        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:BlobConnection"] = await storage.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None)
        });
        hb.AddAzureBlobClient("BlobConnection");

        using var host = hb.Build();
        await host.StartAsync();

        var serviceClient = host.Services.GetRequiredService<BlobServiceClient>();
        var accountInfo = (await serviceClient.GetAccountInfoAsync()).Value;
        Assert.NotNull(accountInfo);
        Assert.Equal(AccountKind.StorageV2, accountInfo.AccountKind);
        Assert.Equal(SkuName.StandardRagrs, accountInfo.SkuName);
    }

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddXunitLogging(testOutputHelper);
        return builder;
    }
}
