// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal sealed partial class ResourceService : IResourceService, IAsyncDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ResourcePublisher _resourcePublisher;
    private readonly ConsoleLogPublisher _consoleLogPublisher;

    public ResourceService(DistributedApplicationModel applicationModel, KubernetesService kubernetesService, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
    {
        ApplicationName = ComputeApplicationName(hostEnvironment.ApplicationName);

        _resourcePublisher = new ResourcePublisher(_cancellationTokenSource.Token);
        _consoleLogPublisher = new ConsoleLogPublisher(_resourcePublisher);

        _ = new DcpDataSource(kubernetesService, applicationModel, loggerFactory, _resourcePublisher.IntegrateAsync, _cancellationTokenSource.Token);

        static string ComputeApplicationName(string applicationName)
        {
            const string AppHostSuffix = ".AppHost";

            if (applicationName.EndsWith(AppHostSuffix, StringComparison.OrdinalIgnoreCase))
            {
                applicationName = applicationName[..^AppHostSuffix.Length];
            }

            return applicationName;
        }
    }

    public string ApplicationName { get; }

    public ResourceSubscription SubscribeResources()
    {
        return _resourcePublisher.Subscribe();
    }

    public IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken)
    {
        var subscription = _consoleLogPublisher.Subscribe(resourceName);

        return subscription is null ? null : Enumerate();

        async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> Enumerate()
        {
            using var token = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);

            await foreach (var group in subscription.WithCancellation(token.Token))
            {
                yield return group;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
    }
}
