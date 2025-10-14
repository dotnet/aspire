// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

}
