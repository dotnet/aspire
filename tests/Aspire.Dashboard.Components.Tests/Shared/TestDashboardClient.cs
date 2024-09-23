// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Components.Tests.Shared;

public class TestDashboardClient : IDashboardClient
{
    private readonly Func<string, Channel<IReadOnlyList<ResourceLogLine>>>? _consoleLogsChannelProvider;
    private readonly Func<Channel<IReadOnlyList<ResourceViewModelChange>>>? _resourceChannelProvider;
    private readonly IList<ResourceViewModel>? _initialResources;

    public bool IsEnabled { get; }
    public Task WhenConnected { get; } = Task.CompletedTask;
    public string ApplicationName { get; } = "TestApp";

    public TestDashboardClient(
        bool? isEnabled = false,
        Func<string, Channel<IReadOnlyList<ResourceLogLine>>>? consoleLogsChannelProvider = null,
        Func<Channel<IReadOnlyList<ResourceViewModelChange>>>? resourceChannelProvider = null,
        IList<ResourceViewModel>? initialResources = null)
    {
        IsEnabled = isEnabled ?? false;
        _consoleLogsChannelProvider = consoleLogsChannelProvider;
        _resourceChannelProvider = resourceChannelProvider;
        _initialResources = initialResources;
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> SubscribeConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_consoleLogsChannelProvider == null)
        {
            throw new InvalidOperationException("No channel provider set.");
        }

        var channel = _consoleLogsChannelProvider(resourceName);

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    public Task<ResourceViewModelSubscription> SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        if (_resourceChannelProvider == null)
        {
            throw new InvalidOperationException("No channel provider set.");
        }

        var channel = _resourceChannelProvider();

        return Task.FromResult(new ResourceViewModelSubscription(_initialResources?.ToImmutableArray() ?? [], BuildSubscription(channel, cancellationToken)));

        async static IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> BuildSubscription(Channel<IReadOnlyList<ResourceViewModelChange>> channel, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return item;
            }
        }
    }
}
