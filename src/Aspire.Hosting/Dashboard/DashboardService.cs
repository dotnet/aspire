// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.ResourceService.Proto.V1;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Implements a gRPC service that a dashboard can consume.
/// </summary>
/// <remarks>
/// An instance of this type is created for every gRPC service call, so it may not hold onto any state
/// required beyond a single request. Longer-scoped data is stored in <see cref="DashboardServiceData"/>.
/// </remarks>
[Authorize(Policy = ResourceServiceApiKeyAuthorization.PolicyName)]
internal sealed partial class DashboardService(DashboardServiceData serviceData, IHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, ILogger<DashboardService> logger)
    : Aspire.ResourceService.Proto.V1.DashboardService.DashboardServiceBase
{
    // Calls that consume or produce streams must create a linked cancellation token
    // with IHostApplicationLifetime.ApplicationStopping to ensure eager cancellation
    // of pending connections during shutdown.

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
        await ExecuteAsync(
            WatchResourcesInternal,
            context).ConfigureAwait(false);

        async Task WatchResourcesInternal(CancellationToken cancellationToken)
        {
            var (initialData, updates) = serviceData.SubscribeResources();

            var data = new InitialResourceData();

            foreach (var resource in initialData)
            {
                data.Resources.Add(Resource.FromSnapshot(resource));
            }

            await responseStream.WriteAsync(new() { InitialData = data }, cancellationToken).ConfigureAwait(false);

            await foreach (var batch in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
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

                await responseStream.WriteAsync(new() { Changes = changes }, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task WatchResourceConsoleLogs(
        WatchResourceConsoleLogsRequest request,
        IServerStreamWriter<WatchResourceConsoleLogsUpdate> responseStream,
        ServerCallContext context)
    {
        await ExecuteAsync(
            WatchResourceConsoleLogsInternal,
            context).ConfigureAwait(false);

        async Task WatchResourceConsoleLogsInternal(CancellationToken cancellationToken)
        {
            var subscription = serviceData.SubscribeConsoleLogs(request.ResourceName);

            if (subscription is null)
            {
                return;
            }

            await foreach (var group in subscription.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var update = new WatchResourceConsoleLogsUpdate();

                foreach (var (lineNumber, content, isErrorMessage) in group)
                {
                    update.LogLines.Add(new ConsoleLogLine() { LineNumber = lineNumber, Text = content, IsStdErr = isErrorMessage });
                }

                await responseStream.WriteAsync(update, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task<ResourceCommandResponse> ExecuteResourceCommand(ResourceCommandRequest request, ServerCallContext context)
    {
        var (result, errorMessage) = await serviceData.ExecuteCommandAsync(request.ResourceName, request.CommandType, context.CancellationToken).ConfigureAwait(false);
        var responseKind = result switch
        {
            DashboardServiceData.ExecuteCommandResult.Success => ResourceCommandResponseKind.Succeeded,
            DashboardServiceData.ExecuteCommandResult.Canceled => ResourceCommandResponseKind.Cancelled,
            DashboardServiceData.ExecuteCommandResult.Failure => ResourceCommandResponseKind.Failed,
            _ => ResourceCommandResponseKind.Undefined
        };

        return new ResourceCommandResponse
        {
            Kind = responseKind,
            ErrorMessage = errorMessage ?? string.Empty
        };
    }

    private async Task ExecuteAsync(Func<CancellationToken, Task> execute, ServerCallContext serverCallContext)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(hostApplicationLifetime.ApplicationStopping, serverCallContext.CancellationToken);

        try
        {
            await execute(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return.
        }
        catch (IOException) when (cts.Token.IsCancellationRequested)
        {
            // Ignore cancellation and just return. Cancelled writes throw IOException.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error executing service method '{serverCallContext.Method}'.");
            throw;
        }
    }
}
