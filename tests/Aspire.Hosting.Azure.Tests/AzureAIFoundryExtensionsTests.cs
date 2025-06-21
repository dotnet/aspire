// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

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
        var expected = "Endpoint=" + resource.AIFoundryApiEndpoint.ValueExpression;
        var connectionString = resource.ConnectionStringExpression.ValueExpression;
        Assert.Equal(expected, connectionString);
    }

    [Fact]
    public void RunAsFoundryLocal_SetsIsLocal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.False(resource.IsLocal);
        Assert.Null(resource.ApiKey);

        var localBuilder = resourceBuilder.RunAsFoundryLocal();
        var localResource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.True(localResource.IsLocal);
    }

    [Fact]
    public void RunAsFoundryLocal_DeploymentIsMarkedLocal()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        var localBuilder = resourceBuilder.RunAsFoundryLocal();
        var localResource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.True(localResource.IsLocal);

        foreach (var deployment in localResource.Deployments)
        {
            Assert.True(deployment.Parent.IsLocal);
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

        var manifest = await AzureManifestUtils.GetManifestWithBicep(foundry.Resource);

        await Verify(manifest.BicepText, extension: "bicep");
    }
}
