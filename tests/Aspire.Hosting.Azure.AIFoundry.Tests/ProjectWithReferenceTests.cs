// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class ProjectWithReferenceTests
{
    [Fact]
    public async Task WithReference_InjectsFoundryProjectEndpointEnvVar()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("test-account")
            .AddProject("test-project");

        var pyapp = builder.AddPythonApp("app", "./app.py", "main:app")
            .WithReference(project);

        builder.Build();
        var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pyapp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        Assert.Contains(envVars, kvp =>
            kvp.Key == "AZURE_AI_FOUNDRY_PROJECT_ENDPOINT"
            && kvp.Value == "{test-project.outputs.endpoint}");
    }

    [Fact]
    public async Task WithReference_InjectsApplicationInsightsConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("test-account")
            .AddProject("test-project");

        var pyapp = builder.AddPythonApp("app", "./app.py", "main:app")
            .WithReference(project);

        builder.Build();
        var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pyapp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        Assert.Contains(envVars, kvp =>
            kvp.Key == "APPLICATIONINSIGHTS_CONNECTION_STRING"
            && kvp.Value == "{test-project.outputs.APPLICATION_INSIGHTS_CONNECTION_STRING}");
    }

    [Fact]
    public async Task WithReference_InjectsStandardConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("test-account")
            .AddProject("test-project");

        var pyapp = builder.AddPythonApp("app", "./app.py", "main:app")
            .WithReference(project);

        builder.Build();
        var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pyapp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        // Standard connection string is also injected via ResourceBuilderExtensions.WithReference
        Assert.Contains(envVars, kvp =>
            kvp.Key.StartsWith("ConnectionStrings__", StringComparison.Ordinal));
    }
}
