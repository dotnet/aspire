// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public class EmptyTemplateRunTests : WorkloadTestsBase, IClassFixture<EmptyTemplateRunFixture>
{
    private readonly EmptyTemplateRunFixture _testFixture;

    public EmptyTemplateRunTests(EmptyTemplateRunFixture fixture, ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _testFixture = fixture;
    }

    [Fact]
    public async Task ResourcesShowUpOnDashboad()
    {
        await using var context = await CreateNewBrowserContextAsync();
        await CheckDashboardHasResourcesAsync(
            await _testFixture.Project!.OpenDashboardPageAsync(context),
            [],
            timeoutSecs: 1_000);
    }
}
