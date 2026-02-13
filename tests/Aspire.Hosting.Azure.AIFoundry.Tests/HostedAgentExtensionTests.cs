// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class HostedAgentExtensionTests
{
    [Fact]
    public void PublishAsHostedAgent_InRunMode_AddsHttpEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var app = builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent(project);

        builder.Build();

        // In run mode, the resource should have an HTTP endpoint annotation
        Assert.True(app.Resource.TryGetEndpoints(out var endpoints));
        Assert.Contains(endpoints, e => e.Name == "http");
    }

    [Fact]
    public void PublishAsHostedAgent_InRunMode_ConfiguresHealthCheck()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent(project);

        builder.Build();

        // The resource should have a health check annotation from WithHttpHealthCheck
        var resource = builder.Resources.Single(r => r.Name == "agent");
        var healthAnnotation = resource.Annotations.OfType<HealthCheckAnnotation>().FirstOrDefault();
        Assert.NotNull(healthAnnotation);
    }

    [Fact]
    public void PublishAsHostedAgent_InPublishMode_ValidatesRegion()
    {
        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish);

        builder.Configuration["Azure:Location"] = "invalidregion";

        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        Assert.Throws<InvalidOperationException>(() =>
            builder.AddPythonApp("agent", "./app.py", "main:app")
                .PublishAsHostedAgent(project));
    }

    [Fact]
    public void PublishAsHostedAgent_InPublishMode_AcceptsValidRegion()
    {
        using var builder = TestDistributedApplicationBuilder.Create(
            DistributedApplicationOperation.Publish);

        builder.Configuration["Azure:Location"] = "eastus";

        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var app = builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent(project);

        Assert.NotNull(app);
    }

    [Fact]
    public void PublishAsHostedAgent_NoRegionConfig_DoesNotThrow()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        var app = builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent(project);

        Assert.NotNull(app);
    }

    [Fact]
    public void PublishAsHostedAgent_InPublishMode_CreatesHostedAgentResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        var project = builder.AddAzureAIFoundry("account")
            .AddProject("my-project");

        builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent(project);

        builder.Build();

        var hostedAgent = builder.Resources.OfType<AzureHostedAgentResource>().SingleOrDefault();
        Assert.NotNull(hostedAgent);
        Assert.Equal("agent-ha", hostedAgent.Name);
    }

    [Fact]
    public void PublishAsHostedAgent_WithoutProject_CreatesDefaultProject()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddPythonApp("agent", "./app.py", "main:app")
            .PublishAsHostedAgent();

        builder.Build();

        // A project should be auto-created
        var project = builder.Resources.OfType<AzureCognitiveServicesProjectResource>().SingleOrDefault();
        Assert.NotNull(project);
    }
}
