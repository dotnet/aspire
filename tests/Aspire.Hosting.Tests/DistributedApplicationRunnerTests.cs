// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationRunnerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void EnsureFailingPublishResultsInRunMethodThrowing()
    {
        var args = new[] { "--publisher", "explodingpublisher" };
        using var builder = TestDistributedApplicationBuilder.Create(outputHelper, args);
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.AddPublisher<ExplodingPublisher, ExplodingPublisherOptions>("explodingpublisher");
#pragma warning restore ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        using var app = builder.Build();

        var ex = Assert.Throws<AggregateException>(app.Run);

        Assert.Collection(
            ex.InnerExceptions,
            e => Assert.Equal("Publishing failed exception message: Boom!", e.Message)
        );
    }
}

internal sealed class ExplodingPublisher : IDistributedApplicationPublisher
{
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Boom!");
    }
}

internal sealed class ExplodingPublisherOptions
{
}