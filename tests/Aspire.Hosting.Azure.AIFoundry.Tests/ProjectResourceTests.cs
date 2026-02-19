// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class ProjectResourceTests
{
    [Fact]
    public void AddProject_CreatesProjectWithCorrectParent()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var account = builder.AddAzureAIFoundry("account");
        var project = account.AddProject("my-project");

        Assert.IsType<AzureCognitiveServicesProjectResource>(project.Resource);
        Assert.Equal("my-project", project.Resource.Name);
        Assert.Same(account.Resource, project.Resource.Parent);
    }

    [Fact]
    public void AddAzureAIFoundryProject_CreatesAccountAndProject()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundryProject("my-project");

        Assert.IsType<AzureCognitiveServicesProjectResource>(project.Resource);
        Assert.Equal("my-project", project.Resource.Name);

        // Should also create a parent account
        var account = builder.Resources.OfType<AzureAIFoundryResource>().SingleOrDefault();
        Assert.NotNull(account);
        Assert.Same(account, project.Resource.Parent);
    }

    [Fact]
    public void ConnectionStringExpression_HasCorrectFormat()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var expr = project.Resource.ConnectionStringExpression;
        Assert.Equal("Endpoint={my-project.outputs.endpoint}", expr.ValueExpression);
    }

    [Fact]
    public void UriExpression_ReferencesEndpointOutput()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var expr = project.Resource.UriExpression;
        Assert.Equal("{my-project.outputs.endpoint}", expr.ValueExpression);
    }

    [Fact]
    public void GetConnectionProperties_ReturnsUriAndConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var properties = ((IResourceWithConnectionString)project.Resource)
            .GetConnectionProperties()
            .ToArray();

        Assert.Equal(3, properties.Length);
        Assert.Equal("Uri", properties[0].Key);
        Assert.Equal("{my-project.outputs.endpoint}", properties[0].Value.ValueExpression);
        Assert.Equal("ConnectionString", properties[1].Key);
        Assert.Equal("ApplicationInsightsConnectionString", properties[2].Key);
        Assert.Equal("{my-project.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING}", properties[2].Value.ValueExpression);
    }

    [Fact]
    public void WithAppInsights_SetsAppInsightsResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var appInsights = builder.AddAzureApplicationInsights("ai");
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project")
            .WithAppInsights(appInsights);

        Assert.Same(appInsights.Resource, project.Resource.AppInsights);
    }

    [Fact]
    public void AddModelDeployment_AddsDeploymentToParentFoundry()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var deployment = project.AddModelDeployment("chat", "gpt-4", "1.0", "OpenAI");

        Assert.NotNull(deployment);
        Assert.Equal("chat", deployment.Resource.Name);
        Assert.Equal("gpt-4", deployment.Resource.ModelName);
    }

    [Fact]
    public void AddModelDeployment_WithModel_AddsDeployment()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var model = new AIFoundryModel { Name = "gpt-4", Version = "1.0", Format = "OpenAI" };
        var deployment = project.AddModelDeployment("chat", model);

        Assert.NotNull(deployment);
        Assert.Equal("chat", deployment.Resource.Name);
    }

    [Fact]
    public void AddAzureAIFoundryProject_ResourceIsCreated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundryProject("my-project");

        // If the resource was created, the basic infrastructure is functional
        Assert.NotNull(project.Resource);
        Assert.IsType<AzureCognitiveServicesProjectResource>(project.Resource);
    }
}
