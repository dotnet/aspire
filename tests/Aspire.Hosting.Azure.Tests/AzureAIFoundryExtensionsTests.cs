// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.Tests;

public class AzureAIFoundryExtensionsTests
{
    [Fact]
    public void AddAzureAIFoundry_ShouldAddResourceToBuilder()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        Assert.NotNull(resourceBuilder);
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.Equal("myAIFoundry", resource.Name);
    }

    [Fact]
    public void AddDeployment_ShouldAddDeploymentToResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var deploymentBuilder = resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        Assert.NotNull(deploymentBuilder);
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        var deployment = Assert.Single(resource.Deployments);
        Assert.Equal("deployment1", deployment.Name);
        Assert.Equal("gpt-4", deployment.ModelName);
        Assert.Equal("1.0", deployment.ModelVersion);
        Assert.Equal("OpenAI", deployment.Format);
    }

    [Fact]
    public void WithProperties_ShouldApplyConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var deploymentBuilder = resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        bool configured = false;
        deploymentBuilder.WithProperties(d =>
        {
            configured = true;
            d.ModelName = "changed";
        });
        Assert.True(configured);
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        var deployment = Assert.Single(resource.Deployments);
        Assert.Equal("changed", deployment.ModelName);
    }

    [Fact]
    public void AddAzureAIFoundry_ConnectionString_IsCorrect()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        // The connection string should reference the aiFoundryApiEndpoint output
        var expected = $"Endpoint={resource.Endpoint.ValueExpression};EndpointAIInference={resource.AIFoundryApiEndpoint.ValueExpression}models";
        var connectionString = resource.ConnectionStringExpression.ValueExpression;
        Assert.Equal(expected, connectionString);
    }

    [Fact]
    public async Task RunAsFoundryLocal_SetsIsEmulator()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.False(resource.IsEmulator);
        Assert.Null(resource.ApiKey);

        var localBuilder = resourceBuilder.RunAsFoundryLocal();

        var localResource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.True(localResource.IsEmulator);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // Wait until it's not in Starting state anymore (started or failed whether the Foundry Local service is setup or not)
        await rns.WaitForResourceAsync(resource.Name, [KnownResourceStates.FailedToStart, KnownResourceStates.Running], cts.Token);

        var foundryManager = app.Services.GetRequiredService<FoundryLocalManager>();

        Assert.Equal(foundryManager.ApiKey, localResource.ApiKey);
    }

    [Fact]
    public void RunAsFoundryLocal_DeploymentIsMarkedLocal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        var localBuilder = resourceBuilder.RunAsFoundryLocal();
        var localResource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.True(localResource.IsEmulator);

        foreach (var deployment in localResource.Deployments)
        {
            Assert.True(deployment.Parent.IsEmulator);
        }
    }

    [Fact]
    public void RunAsFoundryLocal_DeploymentConnectionString_HasModelProperty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var foundry = builder.AddAzureAIFoundry("myAIFoundry");
        var deployment = foundry.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        foundry.RunAsFoundryLocal();
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.Single(resource.Deployments);
        var connectionString = deployment.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Model=gpt-4", connectionString);
        Assert.Contains("DeploymentId=gpt-4", connectionString);
        Assert.Contains("Endpoint=", connectionString);
        Assert.Contains("Key=", connectionString);
    }

    [Fact]
    public async Task AddAzureAIFoundry_GeneratesValidBicep()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        var foundry = builder.AddAzureAIFoundry("foundry");
        var deployment1 = foundry.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        var deployment2 = foundry.AddDeployment("deployment2", "Phi-4", "1.0", "Microsoft");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        var manifest = await AzureManifestUtils.GetManifestWithBicep(model, foundry.Resource);

        var roles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "foundry-roles");
        var rolesManifest = await AzureManifestUtils.GetManifestWithBicep(model, roles);

        await Verify(manifest.BicepText, extension: "bicep")
            .AppendContentAsFile(rolesManifest.BicepText, "bicep");
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureAIFoundryResource()
    {
        // Arrange
        var aiFoundryResource = new AzureAIFoundryResource("test-foundry", _ => { });
        var infrastructure = new AzureResourceInfrastructure(aiFoundryResource, "test-foundry");

        // Act - Call AddAsExistingResource twice
        var firstResult = aiFoundryResource.AddAsExistingResource(infrastructure);
        var secondResult = aiFoundryResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }
}
