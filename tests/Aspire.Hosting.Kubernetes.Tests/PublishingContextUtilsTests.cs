// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aspire.Hosting.Kubernetes.Tests;

public class PublishingContextUtilsTests
{
    [Fact]
    public void GetEnvironmentOutputPath_WithNullOutputPath_ReturnsFallbackPath()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executionContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        var pipelineContext = new PipelineContext(
            model,
            executionContext,
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: null);

        var stepContext = new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = new TestReportingStep()
        };

        var environment = new TestComputeEnvironmentResource("test-env");

        // Act
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, environment);

        // Assert
        Assert.NotNull(outputPath);
        Assert.EndsWith("aspire-output", outputPath);
        Assert.Contains(Environment.CurrentDirectory, outputPath);
    }

    [Fact]
    public void GetEnvironmentOutputPath_WithExplicitOutputPath_ReturnsExplicitPath()
    {
        // Arrange
        var explicitPath = "/tmp/custom-output";
        var builder = DistributedApplication.CreateBuilder();
        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executionContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        var pipelineContext = new PipelineContext(
            model,
            executionContext,
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: explicitPath);

        var stepContext = new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = new TestReportingStep()
        };

        var environment = new TestComputeEnvironmentResource("test-env");

        // Act
        var outputPath = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, environment);

        // Assert
        Assert.Equal(explicitPath, outputPath);
    }

    [Fact]
    public void GetEnvironmentOutputPath_WithMultipleEnvironments_AppendsEnvironmentName()
    {
        // Arrange
        var explicitPath = "/tmp/custom-output";
        var builder = DistributedApplication.CreateBuilder();
        
        var env1 = new TestComputeEnvironmentResource("env1");
        var env2 = new TestComputeEnvironmentResource("env2");
        builder.AddResource(env1);
        builder.AddResource(env2);

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executionContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        var pipelineContext = new PipelineContext(
            model,
            executionContext,
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: explicitPath);

        var stepContext = new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = new TestReportingStep()
        };

        // Act
        var outputPath1 = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, env1);
        var outputPath2 = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, env2);

        // Assert
        Assert.Equal(Path.Combine(explicitPath, "env1"), outputPath1);
        Assert.Equal(Path.Combine(explicitPath, "env2"), outputPath2);
    }

    [Fact]
    public void GetEnvironmentOutputPath_WithMultipleEnvironmentsAndNullOutputPath_UsesFallbackAndAppendsEnvironmentName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        
        var env1 = new TestComputeEnvironmentResource("env1");
        var env2 = new TestComputeEnvironmentResource("env2");
        builder.AddResource(env1);
        builder.AddResource(env2);

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executionContext = app.Services.GetRequiredService<DistributedApplicationExecutionContext>();

        var pipelineContext = new PipelineContext(
            model,
            executionContext,
            app.Services,
            NullLogger.Instance,
            CancellationToken.None,
            outputPath: null);

        var stepContext = new PipelineStepContext
        {
            PipelineContext = pipelineContext,
            ReportingStep = new TestReportingStep()
        };

        // Act
        var outputPath1 = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, env1);
        var outputPath2 = PublishingContextUtils.GetEnvironmentOutputPath(stepContext, env2);

        // Assert
        var expectedBasePath = Path.Combine(Environment.CurrentDirectory, "aspire-output");
        Assert.Equal(Path.Combine(expectedBasePath, "env1"), outputPath1);
        Assert.Equal(Path.Combine(expectedBasePath, "env2"), outputPath2);
    }

    private sealed class TestComputeEnvironmentResource : Resource, IComputeEnvironmentResource
    {
        public TestComputeEnvironmentResource(string name) : base(name)
        {
        }

        public ReferenceExpression GetHostAddressExpression(EndpointReference endpointReference)
        {
            return ReferenceExpression.Create($"{endpointReference.Resource.Name}");
        }
    }

    private sealed class TestReportingStep : IReportingStep
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Log(Microsoft.Extensions.Logging.LogLevel logLevel, string message, bool enableMarkdown = false)
        {
            // No-op
        }

        public Task<IReportingTask> CreateTaskAsync(string statusText, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReportingTask>(new TestReportingTask());
        }

        public Task CompleteAsync(string completionText, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class TestReportingTask : IReportingTask
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task CompleteAsync(string? completionMessage = null, CompletionState completionState = CompletionState.Completed, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(string statusText, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
