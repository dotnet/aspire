// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Models the state for <see cref="DashboardService"/>, as that service is constructed
/// for each gRPC request. This long-lived object holds state across requests.
/// </summary>
internal sealed class DashboardServiceData : IDisposable
{
    private readonly ResourcePublisher _resourcePublisher;
    private readonly ConsoleLogPublisher _consoleLogPublisher;
    private readonly DcpDataSource _dcpDataSource;

    public DashboardServiceData(
        DistributedApplicationModel applicationModel,
        KubernetesService kubernetesService,
        ILoggerFactory loggerFactory)
    {
        _resourcePublisher = new ResourcePublisher();
        _consoleLogPublisher = new ConsoleLogPublisher(_resourcePublisher);
        _dcpDataSource = new DcpDataSource(kubernetesService, applicationModel, loggerFactory, _resourcePublisher.IntegrateAsync);
    }

    public void Dispose()
    {
        _resourcePublisher.Dispose();
        _dcpDataSource.Dispose();
    }

    internal ResourceSnapshotSubscription SubscribeResources()
    {
        return _resourcePublisher.Subscribe();
    }

    internal IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName)
    {
        var sequence = _consoleLogPublisher.Subscribe(resourceName);

        return sequence is null ? null : Enumerate();

        async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in sequence.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}
