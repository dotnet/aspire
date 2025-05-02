// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePublisherTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_GeneratesMainBicep(bool useContext)
    {
        using var tempDir = new TempDirectory();
        // Arrange
        var options = new OptionsMonitor(new AzurePublisherOptions { OutputPath = tempDir.Path });
        var provisionerOptions = Options.Create(new AzureProvisioningOptions());

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // The azure publisher is not tied to azure container apps but this is
        // a good way to test the end to end scenario
        builder.AddAzureContainerAppEnvironment("acaEnv");

        var storageSku = builder.AddParameter("storageSku", "Standard_LRS", publishValueAsDefault: true);
        var description = builder.AddParameter("skuDescription", "The sku is ", publishValueAsDefault: true);
        var skuDescriptionExpr = ReferenceExpression.Create($"{description} {storageSku}");

        var kvName = builder.AddParameter("kvName");
        var kvRg = builder.AddParameter("kvRg", "rg-shared");

        builder.AddAzureKeyVault("kv").AsExisting(kvName, kvRg);

        builder.AddAzureStorage("existing-storage").PublishAsExisting("images", "rg-shared");

        var pgdb = builder.AddAzurePostgresFlexibleServer("pg").AddDatabase("pgdb");
        var cosmos = builder.AddAzureCosmosDB("account").AddCosmosDatabase("db");
        var blobs = builder.AddAzureStorage("storage")
                            .ConfigureInfrastructure(c =>
                            {
                                var storageAccount = c.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault();

                                storageAccount!.Sku.Name = storageSku.AsProvisioningParameter(c);

                                var output = new ProvisioningOutput("description", typeof(string))
                                {
                                    Value = skuDescriptionExpr.AsProvisioningParameter(c, "sku_description")
                                };

                                c.Add(output);
                            })
                            .AddBlobs("blobs");

        builder.AddAzureInfrastructure("mod", infra =>
        {
            // Noop
        })
        .WithParameter("pgdb", pgdb.Resource.ConnectionStringExpression);

        builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                        .WithReference(cosmos);

        builder.AddProject<TestProject>("fe", launchProfileName: null)
                        .WithEnvironment("BLOB_CONTAINER_URL", $"{blobs}/container");

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        if (useContext)
        {
            // tests the public AzurePublishingContext API
            var context = new AzurePublishingContext(
                options.CurrentValue,
                provisionerOptions.Value,
                NullLogger<AzurePublishingContext>.Instance);

            await context.WriteModelAsync(model, default);
        }
        else
        {
            // tests via the internal Publisher object
            var publisher = new AzurePublisher("azure",
                options,
                provisionerOptions,
                NullLogger<AzurePublisher>.Instance);

            await publisher.PublishAsync(model, default);
        }

        Assert.True(File.Exists(Path.Combine(tempDir.Path, "main.bicep")));

        var content = File.ReadAllText(Path.Combine(tempDir.Path, "main.bicep"));

        await Verifier.Verify(content, extension: "bicep")
            .UseHelixAwareDirectory("Snapshots");
    }

    [Fact]
    public async Task AzurePublishingContext_CapturesParametersAndOutputsCorrectly()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureContainerAppEnvironment("acaEnv");

        var storageSku = builder.AddParameter("storage-Sku", "Standard_LRS", publishValueAsDefault: true);
        var description = builder.AddParameter("skuDescription", "The sku is ", publishValueAsDefault: true);
        var skuDescriptionExpr = ReferenceExpression.Create($"{description} {storageSku}");

        var kv = builder.AddAzureKeyVault("kv");
        var cosmos = builder.AddAzureCosmosDB("account").AddCosmosDatabase("db");

        var blobs = builder.AddAzureStorage("storage")
                            .ConfigureInfrastructure(c =>
                            {
                                var storageAccount = c.GetProvisionableResources().OfType<StorageAccount>().FirstOrDefault();

                                storageAccount!.Sku.Name = storageSku.AsProvisioningParameter(c);

                                var output = new ProvisioningOutput("description", typeof(string))
                                {
                                    Value = skuDescriptionExpr.AsProvisioningParameter(c, "sku_description")
                                };

                                c.Add(output);
                            })
                            .AddBlobs("blobs");

        builder.AddProject<TestProject>("fe", launchProfileName: null)
            .WithEnvironment("BLOB_CONTAINER_URL", $"{blobs}/container")
            .WithReference(cosmos);

        var externalResource = new ExternalResourceWithParameters("external")
        {
            Parameters =
            {
                ["kvUri"] = kv.Resource.VaultUri,
                ["blob"] = blobs.Resource.ConnectionStringExpression,
            }
        };
        builder.AddResource(externalResource);

        var app = builder.Build();

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        await ExecuteBeforeStartHooksAsync(app, default);

        var context = new AzurePublishingContext(
            new AzurePublisherOptions { OutputPath = tempDir.Path },
            new AzureProvisioningOptions(),
            NullLogger<AzurePublishingContext>.Instance);

        await context.WriteModelAsync(model, default);

        Assert.Collection(
            context.OutputLookup,
            item =>
            {
                Assert.Equal("acaEnv_AZURE_CONTAINER_REGISTRY_NAME", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("acaEnv_AZURE_CONTAINER_REGISTRY_ENDPOINT", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("acaEnv_AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("acaEnv_AZURE_CONTAINER_APPS_ENVIRONMENT_ID", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("fe_identity_id", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal(new BicepOutputReference("blobEndpoint", blobs.Resource.Parent), item.Key);
                Assert.Equal("storage_blobEndpoint", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal(new BicepOutputReference("connectionString", cosmos.Resource.Parent), item.Key);
                Assert.Equal("account_connectionString", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal("fe_identity_clientId", item.Value.BicepIdentifier);
            },
            item =>
            {
                Assert.Equal(new BicepOutputReference("vaultUri", kv.Resource), item.Key);
                Assert.Equal("kv_vaultUri", item.Value.BicepIdentifier);
            });

        Assert.Collection(
            context.ParameterLookup,
            item =>
            {
                Assert.Equal(storageSku.Resource, item.Key);
                Assert.Equal("storage_Sku", item.Value.BicepIdentifier);
                Assert.Equal("Standard_LRS", item.Value.Value.Value);
            },
            item =>
            {
                Assert.Equal(description.Resource, item.Key);
                Assert.Equal(description.Resource.Name, item.Value.BicepIdentifier);
                Assert.Equal("The sku is ", item.Value.Value.Value);
            });
    }

    private sealed class ExternalResourceWithParameters(string name) : Resource(name), IResourceWithParameters
    {
        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();
    }

    private sealed class OptionsMonitor(AzurePublisherOptions options) : IOptionsMonitor<AzurePublisherOptions>
    {
        public AzurePublisherOptions Get(string? name) => options;

        public IDisposable OnChange(Action<AzurePublisherOptions, string> listener) => null!;

        public AzurePublisherOptions CurrentValue => options;
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory(".aspire-publisers").FullName;
        }

        public string Path { get; }
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
