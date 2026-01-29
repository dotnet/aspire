// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
// using Aspire.Hosting.Azure.AIFoundry;

namespace Aspire.Hosting.Azure.AIFoundry.Tests;

public class AddProjectTests
{
    [Fact]
    public void ShouldCreateProject()
    {
        // Arrange
        const string name = "my-project";
        using var builder = TestDistributedApplicationBuilder.Create();

        // Act
        var resourceBuilder = builder.AddAzureAIFoundry("account")
            .AddProject(name);

        // Assert
        Assert.NotNull(resourceBuilder);
        Assert.NotNull(resourceBuilder.Resource);
        Assert.Equal(name, resourceBuilder.Resource.Name);
        Assert.IsType<AzureCognitiveServicesProjectResource>(resourceBuilder.Resource);
    }

    [Fact]
    public async Task AddProject_WithReference_ShouldBindConnectionStringEnvVar()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("test-account")
            .AddProject("test-project");

        var pyapp = builder.AddPythonApp("app", "./app.py", "main:app")
            .WithReference(project);

        // Act
        builder.Build();
        var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(pyapp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        // Assert
        Assert.Contains(envVars, (kvp) =>
        {
            var (key, value) = kvp;
            return key is "AZURE_AI_PROJECT_ENDPOINT"
                && value is "{test-project.outputs.connectionString}";
        });
    }

    [Fact]
    public async Task AddProject_WithReference()
    {
        // Arrange
        using var builder = TestDistributedApplicationBuilder.Create();
        var project = builder.AddAzureAIFoundry("test-account")
            .AddProject("test-project");

        var pyapp = builder.AddPythonApp("app", "./app.py", "main:app")
            .WithReference(project);

        // Act
        builder.Build();
        var envVars = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(pyapp.Resource, DistributedApplicationOperation.Publish, TestServiceProvider.Instance);

        // Assert
        Assert.Contains(envVars, (kvp) =>
        {
            var (key, value) = kvp;
            Console.WriteLine($"{key}={value}");
            return key is "AZURE_AI_PROJECT_ENDPOINT"
                && value is "{test-project.outputs.connectionString}";
        });
    }
}
