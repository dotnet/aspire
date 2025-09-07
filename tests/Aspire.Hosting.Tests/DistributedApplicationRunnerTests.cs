// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationRunnerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void EnsureFailingPublishResultsInRunMethodThrowing()
    {
        var args = new[] { "--publisher", "explodingpublisher" };
        using var builder = TestDistributedApplicationBuilder.Create(outputHelper, args);
        builder.AddPublisher<ExplodingPublisher, ExplodingPublisherOptions>("explodingpublisher");
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
