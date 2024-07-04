// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// using Aspire.EndToEnd.Tests;
using Aspire.Hosting.Nats;
using Aspire.Workload.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Playground.Tests;

public class NatsTests : PlaygroundTestsBase, IClassFixture<NatsPlaygroundAppFixture>
{
    private readonly NatsPlaygroundAppFixture _testFixture;

    public NatsTests(NatsPlaygroundAppFixture testFixture, ITestOutputHelper testOutput) : base(testOutput)
    {
        _testFixture = testFixture;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public Task ApiServiceIsHealthy(string path)
        => _testFixture.Projects["api"].WaitForHealthyStatusAsync("http", _testOutput, path, CancellationToken.None);

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4623", typeof(PlaywrightProvider), nameof(PlaywrightProvider.DoesNotHavePlaywrightSupport))]
    public async Task ResourcesShowUpOnDashboad()
    {
        await using var context = await CreateNewBrowserContextAsync();
        await CheckDashboardHasResourcesAsync(
            await _testFixture.Project!.OpenDashboardPageAsync(context),
            GetExpectedResources(_testFixture.Project),
            timeoutSecs: 1_000);
    }

    private static List<ResourceRow> GetExpectedResources(AspireProject project)
    {
        _ = project;
        List<ResourceRow> expectedResources =
        [
            new ResourceRow(
                Type: "Project",
                Name: "api",
                State: "Running",
                Source: "Nats.ApiService.csproj",
                Endpoints: ["http://localhost:\\d+", "https://localhost:\\d+"]),

            new ResourceRow(
                Type: "Container",
                Name: "nats",
                State: "Running",
                Source: $"{NatsContainerImageTags.Registry}/{NatsContainerImageTags.Image}:{NatsContainerImageTags.Tag}",
                Endpoints: ["tcp://localhost:\\d+"]),

            new ResourceRow(
                Type: "Project",
                Name: "backend",
                State: "Running",
                Source: "Nats.Backend.csproj",
                Endpoints: ["http://localhost:\\d+", "https://localhost:\\d+"]),
        ];

        return expectedResources;
    }
}
