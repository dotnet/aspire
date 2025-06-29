// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEnvironmentResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task WhenUsedWithAzureContainerAppsEnvironment_GeneratesProperBicep()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Add a container that will use the container app environment
        builder.AddContainer("api", "my-api-image:latest")
            .WithHttpEndpoint();

        // Act
        using var app = builder.Build();
        app.Run();

        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.True(File.Exists(mainBicepPath));
        var mainBicep = File.ReadAllText(mainBicepPath);

        var envBicepPath = Path.Combine(tempDir.FullName, "env", "env.bicep");
        Assert.True(File.Exists(envBicepPath));
        var envBicep = File.ReadAllText(envBicepPath);

        await Verify(mainBicep, "bicep")
            .AppendContentAsFile(envBicep, "bicep");

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task WhenUsedWithAzureContainerAppsEnvironment_RespectsStronglyTypedProperties()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        var locationParam = builder.AddParameter("location", "eastus2");
        var resourceGroupParam = builder.AddParameter("resourceGroup", "my-rg");
        builder.AddAzureEnvironment()
            .WithLocation(locationParam)
            .WithResourceGroup(resourceGroupParam);
        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Add a container that will use the container app environment
        builder.AddContainer("api", "my-api-image:latest")
            .WithHttpEndpoint();

        // Act
        using var app = builder.Build();
        app.Run();

        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.True(File.Exists(mainBicepPath));
        var mainBicep = File.ReadAllText(mainBicepPath);

        await Verify(mainBicep, "bicep");            

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task PublishAsync_GeneratesMainBicep_WithSnapshots()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish,
            publisher: "default",
            outputPath: tempDir.FullName);

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
        builder.AddAzureInfrastructure("mod", infra => { })
            .WithParameter("pgdb", pgdb.Resource.ConnectionStringExpression);
        builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                        .WithReference(cosmos);
        builder.AddProject<TestProject>("fe", launchProfileName: null)
                        .WithEnvironment("BLOB_CONTAINER_URL", $"{blobs}/container");

        var app = builder.Build();
        app.Run();

        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.True(File.Exists(mainBicepPath));
        var content = File.ReadAllText(mainBicepPath);

        await Verify(content, extension: "bicep");
    }

    [Fact]
    public async Task AzurePublishingContext_CapturesParametersAndOutputsCorrectly_WithSnapshot()
    {
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish,
            publisher: "default",
            outputPath: tempDir.FullName);
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
        app.Run();

        var mainBicep = File.ReadAllText(Path.Combine(tempDir.FullName, "main.bicep"));
        var storageBicep = File.ReadAllText(Path.Combine(tempDir.FullName, "storage", "storage.bicep"));

        await Verify(mainBicep, "bicep")
            .AppendContentAsFile(storageBicep, "bicep");
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }

    [Fact]
    public async Task AzurePublishingContext_IgnoresAzureBicepResourcesWithIgnoreAnnotation()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-ignore-annotation-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish,
            publisher: "default",
            outputPath: tempDir.FullName);

        // Add Azure container app environment to ensure we have a publishing context
        builder.AddAzureContainerAppEnvironment("acaEnv");

        // Add an Azure storage resource that will be included
        var includedStorage = builder.AddAzureStorage("included-storage");
        
        // Add an Azure storage resource that will be excluded
        var excludedStorage = builder.AddAzureStorage("excluded-storage")
            .ExcludeFromManifest(); // This should be ignored during publishing

        // Act
        using var app = builder.Build();
        app.Run();

        // Assert - Verify the generated bicep files
        var mainBicepPath = Path.Combine(tempDir.FullName, "main.bicep");
        Assert.True(File.Exists(mainBicepPath));
        var mainBicep = File.ReadAllText(mainBicepPath);

        // Check if included-storage bicep file was generated
        var includedStorageBicepPath = Path.Combine(tempDir.FullName, "included-storage", "included-storage.bicep");
        var includedStorageBicep = File.Exists(includedStorageBicepPath) ? File.ReadAllText(includedStorageBicepPath) : "";

        // Verify that excluded-storage bicep file was NOT generated
        var excludedStorageBicepPath = Path.Combine(tempDir.FullName, "excluded-storage", "excluded-storage.bicep");
        Assert.False(File.Exists(excludedStorageBicepPath), "Excluded storage should not have a bicep file generated");

        await Verify(mainBicep, "bicep")
            .AppendContentAsFile(includedStorageBicep, "bicep");

        tempDir.Delete(recursive: true);
    }

    private sealed class ExternalResourceWithParameters(string name) : Resource(name), IResourceWithParameters
    {
        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();
    }
}
