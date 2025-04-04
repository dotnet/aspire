// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.KeyVault;
using Xunit;

namespace Aspire.Hosting.Azure.Tests;

public class AzureProvisioningResourceTests
{
    [Fact]
    public async Task VerifyOutputsAlign()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var resource1 = builder.AddAzureInfrastructure("resource1", infra =>
        {
            var kv = new KeyVaultService("kv");
            infra.Add(kv);

            infra.Add(new ProvisioningOutput("vaultUri", typeof(string))
            {
                Value = kv.Properties.VaultUri
            });

            infra.Add(new ProvisioningOutput("name", typeof(string)) { Value = kv.Name });
        });

        await AzureManifestUtils.GetManifestWithBicep(resource1.Resource);

        Assert.Collection(resource1.Resource.Outputs,
            output =>
            {
                Assert.Equal("vaultUri", output.Key);
                Assert.Null(output.Value);
            },
            output =>
            {
                Assert.Equal("name", output.Key);
                Assert.Null(output.Value);
            });
    }
}
