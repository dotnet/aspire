// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Tests.Shared.DashboardModel;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Tests.Integration.Playwright.Infrastructure;

public sealed class MockDashboardClientStatus : IDashboardClientStatus
{
    public bool IsEnabled => true;
}

public sealed class MockDashboardClient : IDashboardClient
{
    public MockDashboardClient(IDashboardClientStatus dashboardClientStatus)
    {
        _dashboardClientStatus = dashboardClientStatus;
    }

    public static readonly ResourceViewModel TestResource1 = ModelTestHelpers.CreateResource(
        appName: "TestResource",
        resourceType: KnownResourceTypes.Project,
        properties: new[]
        {
            new KeyValuePair<string, ResourcePropertyViewModel>(
                KnownProperties.Project.Path,
                new ResourcePropertyViewModel(
                    KnownProperties.Project.Path,
                    new Value()
                    {
                        StringValue = "C:/MyProjectPath/Project.csproj"
                    },
                    isValueSensitive: false,
                    knownProperty: new(KnownProperties.Project.Path, loc => "Path"),
                    priority: 0))
        }.ToDictionary(),
        state: KnownResourceState.Running);

    private readonly IDashboardClientStatus _dashboardClientStatus;

    public bool IsEnabled => _dashboardClientStatus.IsEnabled;
    public Task WhenConnected => Task.CompletedTask;
    public string ApplicationName => "IntegrationTestApplication";
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
    public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> GetConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ResourceViewModelSubscription(
            [TestResource1],
            Test()
        ));
    }

    private static async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> Test()
    {
        await Task.CompletedTask;
        yield return [];
    }
}
