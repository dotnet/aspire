// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.V1;
using Grpc.Core;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Implements a gRPC service that a dashboard can consume.
/// </summary>
/// <remarks>
/// An instance of this type is created for every gRPC service call, so it may not hold onto any state
/// required beyond a single request. Longer-scoped data is stored in <see cref="DashboardServiceData"/>.
/// </remarks>
internal sealed partial class DashboardService(DashboardServiceData serviceData, IHostEnvironment hostEnvironment)
    : V1.DashboardService.DashboardServiceBase
{
    // TODO implement command handling

    [GeneratedRegex("""^(?<name>.+?)\.?AppHost$""", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ApplicationNameRegex();

    public override Task<ApplicationInformationResponse> GetApplicationInformation(
        ApplicationInformationRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(new ApplicationInformationResponse
        {
            ApplicationName = ComputeApplicationName(hostEnvironment.ApplicationName)
        });

        static string ComputeApplicationName(string applicationName)
        {
            return ApplicationNameRegex().Match(applicationName) switch
            {
                Match { Success: true } match => match.Groups["name"].Value,
                _ => applicationName
            };
        }
    }

    public override async Task WatchResources(
        WatchResourcesRequest request,
        IServerStreamWriter<WatchResourcesUpdate> responseStream,
        ServerCallContext context)
    {
        try
        {
            await WatchResourcesInternal().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException && context.CancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Note that cancelled writes throw IOException.
        }

        async Task WatchResourcesInternal()
        {
            var (initialData, updates) = serviceData.SubscribeResources();

            var data = new InitialResourceData();

            foreach (var resource in initialData)
            {
                data.Resources.Add(Resource.FromSnapshot(resource));
            }

            await responseStream.WriteAsync(new() { InitialData = data }).ConfigureAwait(false);

            await foreach (var batch in updates.WithCancellation(context.CancellationToken))
            {
                WatchResourcesChanges changes = new();

                foreach (var update in batch)
                {
                    var change = new WatchResourcesChange();

                    if (update.ChangeType is ResourceSnapshotChangeType.Upsert)
                    {
                        change.Upsert = Resource.FromSnapshot(update.Resource);
                    }
                    else if (update.ChangeType is ResourceSnapshotChangeType.Delete)
                    {
                        change.Delete = new() { ResourceName = update.Resource.Name, ResourceType = update.Resource.ResourceType };
                    }
                    else
                    {
                        throw new FormatException($"Unexpected {nameof(ResourceSnapshotChange)} type: {update.ChangeType}");
                    }

                    changes.Value.Add(change);
                }

                await responseStream.WriteAsync(new() { Changes = changes }, context.CancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task WatchResourceConsoleLogs(
        WatchResourceConsoleLogsRequest request,
        IServerStreamWriter<WatchResourceConsoleLogsUpdate> responseStream,
        ServerCallContext context)
    {
        try
        {
            await WatchResourceConsoleLogsInternal().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException && context.CancellationToken.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Note that cancelled writes throw IOException.
        }

        async Task WatchResourceConsoleLogsInternal()
        {
            var subscription = serviceData.SubscribeConsoleLogs(request.ResourceName);

            if (subscription is null)
            {
                return;
            }

            await foreach (var group in subscription.WithCancellation(context.CancellationToken))
            {
                WatchResourceConsoleLogsUpdate update = new();

                foreach (var (content, isErrorMessage) in group)
                {
                    update.LogLines.Add(new ConsoleLogLine() { Text = content, IsStdErr = isErrorMessage });
                }

                await responseStream.WriteAsync(update, context.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
