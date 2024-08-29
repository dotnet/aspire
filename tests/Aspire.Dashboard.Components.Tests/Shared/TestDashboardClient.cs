// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Shared;

public class TestDashboardClient : IDashboardClient
{
    public bool IsEnabled { get; }
    public Task WhenConnected { get; } = Task.CompletedTask;
    public string ApplicationName { get; } = "TestApp";

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
