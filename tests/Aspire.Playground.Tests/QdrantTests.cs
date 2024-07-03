// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Qdrant;
using Aspire.Workload.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Playground.Tests;

public class QdrantTests : PlaygroundTestsBase, IClassFixture<QdrantPlaygroundAppFixture>
{
    private readonly QdrantPlaygroundAppFixture _testFixture;

    public QdrantTests(QdrantPlaygroundAppFixture testFixture, ITestOutputHelper testOutput) : base(testOutput)
    {
        _testFixture = testFixture;
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public Task ApiServiceIsHealthy(string path)
        => _testFixture.Projects["apiservice"].WaitForHealthyStatusAsync("http", _testOutput, path, CancellationToken.None);

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
                Name: "apiservice",
                State: "Running",
                Source: "Qdrant.ApiService.csproj",
                Endpoints: ["http://localhost:\\d+", "https://localhost:\\d+"]),

            new ResourceRow(
                Type: "Container",
                Name: "qdrant",
                State: "Running",
                Source: $"{QdrantContainerImageTags.Registry}/{QdrantContainerImageTags.Image}:{QdrantContainerImageTags.Tag}",
                Endpoints: ["tcp://localhost:\\d+"]),

            new ResourceRow(
                Type: "Executable",
                Name: "aspire-dashboard",
                State: "Running",
                Source: null,
                Endpoints: ["None"])
        ];

        return expectedResources;
    }
}

public sealed class QdrantPlaygroundAppFixture : PlaygroundAppFixture
{
    public QdrantPlaygroundAppFixture(IMessageSink diagnosticMessageSink)
        : base ("Qdrant/Qdrant.AppHost", diagnosticMessageSink)
    {
    }
}
