// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureKeyVaultTests
{
    [Fact]
    public async Task AddKeyVaultViaRunMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var mykv = builder.AddAzureKeyVault("mykv");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(mykv.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{mykv.outputs.vaultUri}",
              "path": "mykv.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verifier.Verify(manifest.BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }

    [Fact]
    public async Task AddKeyVaultViaPublishMode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var mykv = builder.AddAzureKeyVault("mykv");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await AzureManifestUtils.GetManifestWithBicep(model, mykv.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{mykv.outputs.vaultUri}",
              "path": "mykv.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verifier.Verify(manifest.BicepText, extension: "bicep")
            .UseFileName($"{nameof(AzureKeyVaultTests)}.{nameof(AddKeyVaultViaPublishMode)}.main")
            .UseHelixAwareDirectory("Snapshots");

        var kvRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "mykv-roles");
        var kvRolesManifest = await AzureManifestUtils.GetManifestWithBicep(kvRoles, skipPreparer: true);
        await Verifier.Verify(kvRolesManifest.BicepText, extension: "bicep")
            .UseFileName($"{nameof(AzureKeyVaultTests)}.{nameof(AddKeyVaultViaPublishMode)}.kvroles")
            .UseHelixAwareDirectory("Snapshots");
    }

    [Fact]
    public async Task WithEnvironment_AddsKeyVaultSecretReference()
    {
        // Arrange: Create a test application builder.
        using var builder = TestDistributedApplicationBuilder.Create();

        // Add a key vault resource.
        var kv = builder.AddAzureKeyVault("myKeyVault");

        kv.Resource.SecretResolver = (s, ct) =>
        {
            return Task.FromResult<string?>("my secret value");
        };

        // Get a secret reference from the key vault resource.
        var secretReference = kv.Resource.GetSecret("mySecret");

        // Add a container resource that supports environment variables.
        var containerBuilder = builder.AddContainer("myContainer", "nginx")
                                       .WithEnvironment("MY_SECRET", secretReference);

        var runEnv = await containerBuilder.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Run);
        var publishEnv = await containerBuilder.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);

        var runKvp = Assert.Single(runEnv);
        var pubishKvp = Assert.Single(publishEnv);

        Assert.Equal("MY_SECRET", runKvp.Key);
        Assert.Same("my secret value", runKvp.Value);

        Assert.Equal("MY_SECRET", pubishKvp.Key);
        Assert.Equal("{myKeyVault.secrets.mySecret}", pubishKvp.Value);
    }

    [Fact]
    public async Task ConsumingAKeyVaultSecretInAnotherBicepModule()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var kv = builder.AddAzureKeyVault("myKeyVault");

        var secretReference = kv.Resource.GetSecret("mySecret");
        var secretReference2 = kv.Resource.GetSecret("mySecret2");

        var module = builder.AddAzureInfrastructure("mymodule", infra =>
        {
            var secret = secretReference.AsKeyVaultSecret(infra);
            var secret2 = secretReference2.AsKeyVaultSecret(infra);

            // Should be idempotent
            _ = secretReference.AsKeyVaultSecret(infra);

            infra.Add(new ProvisioningOutput("secretUri1", typeof(string))
            {
                Value = secret.Properties.SecretUri
            });

            infra.Add(new ProvisioningOutput("secretUri2", typeof(string))
            {
                Value = secret2.Properties.SecretUri
            });
        });

        var manifest = await AzureManifestUtils.GetManifestWithBicep(module.Resource, skipPreparer: true);

        var expectedManifest =
            """
            {
              "type": "azure.bicep.v0",
              "path": "mymodule.module.bicep",
              "params": {
                "mykeyvault_outputs_name": "{myKeyVault.outputs.name}"
              }
            }
            """;

        var m = manifest.ManifestNode.ToString();
        Assert.Equal(expectedManifest, m);

        await Verifier.Verify(manifest.BicepText, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }
}
