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
    private readonly ResourceCollection _resourceCollection;

    public ResourceService(
        DistributedApplicationModel applicationModel, KubernetesService kubernetesService, IHostEnvironment hostEnvironment, ILoggerFactory loggerFactory)
    {
        ApplicationName = ComputeApplicationName(hostEnvironment.ApplicationName);

        _resourceCollection = new ResourceCollection(_cancellationTokenSource.Token);

        _ = new KubernetesDataSource(kubernetesService, applicationModel, loggerFactory, _resourceCollection.Integrate, _cancellationTokenSource.Token);

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

    public ResourceSubscription Subscribe() => _resourceCollection.Subscribe();

    public async ValueTask DisposeAsync()
    {
        // NOTE we don't complete channel writers, nor wait for channel readers to complete.
        // Instead we signal cancellation, which causes both processes to halt immediately.
        // Any pending messages will be collected along with this class, which is being disposed
        // right now. We don't have to clean anything up for the channels.

        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
    }
}
