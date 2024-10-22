// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Dashboard.ConsoleLogs;
using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class LogsControllerTests
{
    [Fact]
    public async Task TestGetLogs_ReturnsCorrectLogsAsync()
    {
        var logsController = new LogsController(new MockDashboardClient());
        var response = await logsController.GetLogsForResource("test");
        Assert.Equal("line 1\nline 2", response.Value);
    }

    private sealed class MockDashboardClient : IDashboardClient
    {
        public ValueTask DisposeAsync() => throw new NotImplementedException();
        public bool IsEnabled { get; } = true;
        public Task WhenConnected { get; } = Task.CompletedTask;
        public string ApplicationName { get; } = "TestApp";
        public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
        public IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken) => throw new NotImplementedException();

        public async IAsyncEnumerable<ResourceLogLine> GetConsoleLogsAsync(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new ResourceLogLine(1, "line 1", false);
            yield return new ResourceLogLine(2, "line 2", true);

            await Task.CompletedTask;
        }
    }
}
