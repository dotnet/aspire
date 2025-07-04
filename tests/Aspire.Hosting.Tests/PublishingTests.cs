// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests;

public class PublishingTests
{
    [Fact]
    public void PublishCallsPublishingCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        var publishedCalled = false;

        builder.AddContainer("cache", "redis")
               .WithPublishingCallback(context =>
                {
                    Assert.NotNull(context);
                    Assert.NotNull(context.Services);
                    Assert.True(context.CancellationToken.CanBeCanceled);
                    Assert.Equal(DistributedApplicationOperation.Publish, context.ExecutionContext.Operation);
                    Assert.Equal("default", context.ExecutionContext.PublisherName);
                    Assert.True(Path.IsPathFullyQualified(context.OutputPath));
                    publishedCalled = true;
                    return Task.CompletedTask;
                });

        using var app = builder.Build();
        app.Run();

        Assert.True(publishedCalled, "Publishing callback was not called.");
    }

    [Fact]
    public void PublishWithDeployFalseDoesNotCallDeployingCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to false
        builder.Configuration["Publishing:Deploy"] = "false";

        var publishingCalled = false;
        var deployingCalled = false;

        builder.AddContainer("cache", "redis")
               .WithPublishingCallback(context =>
                {
                    publishingCalled = true;
                    return Task.CompletedTask;
                })
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    deployingCalled = true;
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.True(publishingCalled, "Publishing callback was not called.");
        Assert.False(deployingCalled, "Deploying callback should not be called when Deploy is false.");
    }

    [Fact]
    public void PublishWithDeployTrueCallsDeployingCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to true
        builder.Configuration["Publishing:Deploy"] = "true";

        var publishingCalled = false;
        var deployingCalled = false;

        builder.AddContainer("cache", "redis")
               .WithPublishingCallback(context =>
                {
                    Assert.NotNull(context);
                    Assert.NotNull(context.Services);
                    Assert.True(context.CancellationToken.CanBeCanceled);
                    Assert.Equal(DistributedApplicationOperation.Publish, context.ExecutionContext.Operation);
                    Assert.Equal("default", context.ExecutionContext.PublisherName);
                    Assert.True(Path.IsPathFullyQualified(context.OutputPath));
                    publishingCalled = true;
                    return Task.CompletedTask;
                })
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    Assert.NotNull(context);
                    Assert.NotNull(context.Services);
                    Assert.True(context.CancellationToken.CanBeCanceled);
                    Assert.Equal(DistributedApplicationOperation.Publish, context.ExecutionContext.Operation);
                    Assert.Equal("default", context.ExecutionContext.PublisherName);
                    Assert.True(Path.IsPathFullyQualified(context.OutputPath));
                    deployingCalled = true;
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.True(publishingCalled, "Publishing callback was not called.");
        Assert.True(deployingCalled, "Deploying callback was not called when Deploy is true.");
    }

    [Fact]
    public void DeployingCallbacksAreCalledAfterPublishingCallbacks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to true
        builder.Configuration["Publishing:Deploy"] = "true";

        var callOrder = new List<string>();

        builder.AddContainer("cache", "redis")
               .WithPublishingCallback(context =>
                {
                    callOrder.Add("publishing");
                    return Task.CompletedTask;
                })
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    callOrder.Add("deploying");
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.Equal(2, callOrder.Count);
        Assert.Equal("publishing", callOrder[0]);
        Assert.Equal("deploying", callOrder[1]);
    }

    [Fact]
    public void MultipleResourcesWithDeployingCallbacks()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to true
        builder.Configuration["Publishing:Deploy"] = "true";

        var deployingCallbacks = new List<string>();

        builder.AddContainer("cache", "redis")
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    deployingCallbacks.Add("cache");
                    return Task.CompletedTask;
                }));

        builder.AddContainer("db", "postgres")
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    deployingCallbacks.Add("db");
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.Equal(2, deployingCallbacks.Count);
        Assert.Contains("cache", deployingCallbacks);
        Assert.Contains("db", deployingCallbacks);
    }

    [Fact]
    public void DeployingCallbackReceivesCorrectContext()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to true
        builder.Configuration["Publishing:Deploy"] = "true";

        var contextValidated = false;

        builder.AddContainer("cache", "redis")
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    // Validate all properties of the DeployingContext
                    Assert.NotNull(context);
                    Assert.NotNull(context.Model);
                    Assert.NotNull(context.Services);
                    Assert.NotNull(context.Logger);
                    Assert.True(context.CancellationToken.CanBeCanceled);
                    Assert.Equal(DistributedApplicationOperation.Publish, context.ExecutionContext.Operation);
                    Assert.Equal("default", context.ExecutionContext.PublisherName);
                    Assert.True(Path.IsPathFullyQualified(context.OutputPath));

                    // Verify the model contains our resource
                    Assert.Single(context.Model.Resources);
                    Assert.Equal("cache", context.Model.Resources.Single().Name);

                    contextValidated = true;
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.True(contextValidated, "DeployingContext validation failed.");
    }

    [Fact]
    public void PublishingOptionsDeployPropertyDefaultsToFalse()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");
        using var app = builder.Build();

        var publishingOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.False(publishingOptions.Value.Deploy, "Deploy should default to false.");
    }

    [Fact]
    public void PublishingOptionsDeployPropertyCanBeSetToTrue()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");
        builder.Configuration["Publishing:Deploy"] = "true";
        using var app = builder.Build();

        var publishingOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.True(publishingOptions.Value.Deploy, "Deploy should be true when configured.");
    }

    [Fact]
    public void PublishingOptionsDeployPropertyCanBeSetViaCommandLine()
    {
        var args = new[] { "--publisher", "default", "--output-path", "./", "--deploy", "true" };
        using var builder = TestDistributedApplicationBuilder.Create(args);
        using var app = builder.Build();

        var publishingOptions = app.Services.GetRequiredService<IOptions<PublishingOptions>>();
        Assert.True(publishingOptions.Value.Deploy, "Deploy should be true when set via command line.");
    }

    [Fact]
    public async Task DeployingCallback_Throws_PropagatesException()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");
        builder.Configuration["Publishing:Deploy"] = "true";
        builder.AddContainer("cache", "redis")
               .WithAnnotation(new DeployingCallbackAnnotation(_ => throw new InvalidOperationException("Deploy failed!")));
        using var app = builder.Build();
        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(() => app.RunAsync());
        Assert.Contains("Deploy failed!", ex.Message);
    }

    [Fact]
    public void DeployingCallback_OnlyLastAnnotationIsUsed()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");
        builder.Configuration["Publishing:Deploy"] = "true";
        var called = string.Empty;
        builder.AddContainer("cache", "redis")
               .WithAnnotation(new DeployingCallbackAnnotation(_ => { called = "first"; return Task.CompletedTask; }))
               .WithAnnotation(new DeployingCallbackAnnotation(_ => { called = "second"; return Task.CompletedTask; }));
        using var app = builder.Build();
        app.Run();
        Assert.Equal("second", called);
    }

    [Fact]
    public void DeployingContextActivityReporterProperty()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default");

        // Explicitly set Deploy to true
        builder.Configuration["Publishing:Deploy"] = "true";

        var activityReporterAccessed = false;

        builder.AddContainer("cache", "redis")
               .WithAnnotation(new DeployingCallbackAnnotation(context =>
                {
                    // Verify that ActivityReporter property is accessible and not null
                    Assert.NotNull(context.ActivityReporter);
                    Assert.IsAssignableFrom<IPublishingActivityReporter>(context.ActivityReporter);
                    
                    // Verify that accessing it multiple times returns the same instance (lazy initialization)
                    var reporter1 = context.ActivityReporter;
                    var reporter2 = context.ActivityReporter;
                    Assert.Same(reporter1, reporter2);
                    
                    activityReporterAccessed = true;
                    return Task.CompletedTask;
                }));

        using var app = builder.Build();
        app.Run();

        Assert.True(activityReporterAccessed, "ActivityReporter property was not tested.");
    }
}
