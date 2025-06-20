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
        var expected = "Endpoint=" + resource.AIFoundryApiEndpoint;
        Assert.Equal(expected, resource.ConnectionStringExpression.ToString());
    }

    [Fact]
    public void AddDeployment_ConnectionString_IsCorrect()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var deploymentBuilder = resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        var deployment = Assert.Single(resource.Deployments);
        // The deployment connection string should match the parent resource's connection string
        Assert.Equal(resource.ConnectionStringExpression.ToString(), deployment.ConnectionStringExpression.ToString());
    }

    [Fact]
    public void RunAsFoundryLocal_SetsIsLocalAndApiKey()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.False(resource.IsLocal);
        Assert.Null(resource.ApiKey);

        var localBuilder = resourceBuilder.RunAsFoundryLocal();
        var localResource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        Assert.True(localResource.IsLocal);
        // ApiKey is set at runtime by FoundryLocalManager, so we only check IsLocal here
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
        // All deployments should have Parent.IsLocal == true
        foreach (var deployment in localResource.Deployments)
        {
            Assert.True(deployment.Parent.IsLocal);
        }
    }

    [Fact]
    public void RunAsFoundryLocal_DeploymentConnectionString_HasModelProperty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var resourceBuilder = builder.AddAzureAIFoundry("myAIFoundry");
        var deploymentBuilder = resourceBuilder.AddDeployment("deployment1", "gpt-4", "1.0", "OpenAI");
        resourceBuilder.RunAsFoundryLocal();
        var resource = Assert.Single(builder.Resources.OfType<AzureAIFoundryResource>());
        var deployment = Assert.Single(resource.Deployments);
        var connectionString = deployment.ConnectionStringExpression.ToString();
        Assert.Contains("Model=deployment1", connectionString);
        Assert.Contains("DeploymentId=deployment1", connectionString);
        Assert.Contains("Endpoint=", connectionString);
        Assert.Contains("Key=", connectionString);
    }
}
