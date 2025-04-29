// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DistributedApplicationRunnerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void EnsureFailingPublishResultsInNonZeroExitCode()
    {
        var args = new[] { "--publisher", "explodingpublisher" };
        using var builder = TestDistributedApplicationBuilder.Create(outputHelper, args);
        using var app = builder.Build();

        Assert.Equal(0, Environment.ExitCode);

        app.Run();

        Assert.Equal(AppHostExitCodes.PublishFailure, Environment.ExitCode);
    }
}

internal sealed class ExplodingPublisher : IDistributedApplicationPublisher
{
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Boom!");
    }
}