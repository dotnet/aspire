// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Azure.Provisioning;
using Azure.Provisioning.Storage;
using Microsoft.Extensions.DependencyInjection;

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
    public void AzurePublishingContext_IgnoresAzureBicepResourcesWithIgnoreAnnotation()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Add two Azure storage resources - one will be ignored, one will not
        var includedStorage = builder.AddAzureStorage("included-storage");
        var excludedStorage = builder.AddAzureStorage("excluded-storage")
            .ExcludeFromManifest(); // This should be ignored during publishing

        // Build the model
        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act - Get the AzureBicepResources using the same logic from AzurePublishingContext
        var allBicepResources = model.Resources.OfType<AzureBicepResource>().ToList();
        var filteredBicepResources = model.Resources.OfType<AzureBicepResource>()
            .Where(r => !r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) || lastAnnotation != ManifestPublishingCallbackAnnotation.Ignore)
            .ToList();

        // Assert
        Assert.Equal(2, allBicepResources.Count);
        Assert.Single(filteredBicepResources);
        Assert.Equal("included-storage", filteredBicepResources.Single().Name);
        
        // Verify the excluded resource is not in the filtered list
        Assert.DoesNotContain(filteredBicepResources, r => r.Name == "excluded-storage");
        
        // Verify that the excluded resource has the ignore annotation
        var excludedBicepResource = allBicepResources.Single(r => r.Name == "excluded-storage");
        Assert.True(excludedBicepResource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var annotation));
        Assert.Same(ManifestPublishingCallbackAnnotation.Ignore, annotation);
    }

    private sealed class ExternalResourceWithParameters(string name) : Resource(name), IResourceWithParameters
    {
        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>();
    }
}
