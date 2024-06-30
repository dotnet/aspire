// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.EndToEnd.Tests;
using Aspire.Hosting.MongoDB;
using Aspire.Playground.Tests;
using Aspire.Workload.Tests;
using Xunit;
using Xunit.Abstractions;

public class MongoDbTests : PlaygroundTestsBase, IClassFixture<MongoPlaygroundAppFixture>
{
    private readonly MongoPlaygroundAppFixture _testFixture;

    public MongoDbTests(MongoPlaygroundAppFixture testFixture, ITestOutputHelper testOutput) : base(testOutput)
    {
        _testFixture = testFixture;
    }

    [Fact]
    public async Task Simple()
    {
        await Task.CompletedTask;
    }

    [Fact]
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
        List<ResourceRow> expectedResources = new()
        {
            new ResourceRow(
                Type: "Project",
                Name: "api",
                State: "Running",
                Source: "Mongo.ApiService.csproj",
                Endpoints: ["http://localhost:\\d+"]),

            new ResourceRow(
                Type: "Container",
                Name: "mongo",
                State: "Running",
                Source: $"{MongoDBContainerImageTags.Registry}/{MongoDBContainerImageTags.Image}:{MongoDBContainerImageTags.Tag}",
                Endpoints: ["tcp://localhost:\\d+"]),

            new ResourceRow(
                Type: "Container",
                Name: "mongo-mongoexpress",
                State: "Running",
                Source: $"{MongoDBContainerImageTags.MongoExpressRegistry}/{MongoDBContainerImageTags.MongoExpressImage}:{MongoDBContainerImageTags.MongoExpressTag}",
                Endpoints: ["http://localhost:\\d+"]),

            new ResourceRow(
                Type: "Executable",
                Name: "aspire-dashboard",
                State: "Running",
                Source: null,
                Endpoints: ["None"])
        };

        return expectedResources;
    }
}
