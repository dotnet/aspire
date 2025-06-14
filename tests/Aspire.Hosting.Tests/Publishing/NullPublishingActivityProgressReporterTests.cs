// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Tests.Publishing;

public class NullPublishingActivityProgressReporterTests
{
    [Fact]
    public async Task CanUseNullReporter()
    {
        var reporter = NullPublishingActivityProgressReporter.Instance;
        var activity = await reporter.CreateActivityAsync("1", "initial", isPrimary: true, default);
        await reporter.UpdateActivityStatusAsync(activity, (status) => status with { IsComplete = true }, default);

        Assert.NotNull(activity);
    }
}
