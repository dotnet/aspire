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
    // gRPC has a maximum receive size of 4MB. Force logs into batches to avoid exceeding receive size.
    // Protobuf sends strings as UTF8. Be conservative and assume the average character byte size is 2.
    public const int LogMaxBatchCharacters = 1024 * 1024 * 2;

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
                var changes = new WatchResourcesChanges();

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
            cancellationToken => WatchResourceConsoleLogsInternal(request.SuppressFollow, cancellationToken),
            context).ConfigureAwait(false);

        async Task WatchResourceConsoleLogsInternal(bool suppressFollow, CancellationToken cancellationToken)
        {
            var enumerable = suppressFollow
                ? serviceData.GetConsoleLogs(request.ResourceName)
                : serviceData.SubscribeConsoleLogs(request.ResourceName);

            if (enumerable is null)
            {
                return;
            }

            await foreach (var group in enumerable.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var sentLines = 0;

                while (sentLines < group.Count)
                {
                    var update = new WatchResourceConsoleLogsUpdate();
                    var currentChars = 0;

                    foreach (var (lineNumber, content, isErrorMessage) in group.Skip(sentLines))
                    {
                        // Truncate excessively long lines.
                        var resolvedContent = content.Length > LogMaxBatchCharacters
                            ? content[..LogMaxBatchCharacters]
                            : content;

                        // Count number of characters to figure out if batch exceeds the limit.
                        // We could calculate byte size here with UTF8 encoding, but getting the exact size of the text and message
                        // would be a bit more complicated. Character count plus a conservative limit should be fine.
                        currentChars += resolvedContent.Length;

                        if (currentChars <= LogMaxBatchCharacters)
                        {
                            update.LogLines.Add(new ConsoleLogLine() { LineNumber = lineNumber, Text = resolvedContent, IsStdErr = isErrorMessage });
                            sentLines++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    await responseStream.WriteAsync(update, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public override async Task<ResourceCommandResponse> ExecuteResourceCommand(ResourceCommandRequest request, ServerCallContext context)
    {
        var (result, errorMessage) = await serviceData.ExecuteCommandAsync(request.ResourceName, request.CommandName, context.CancellationToken).ConfigureAwait(false);
        var responseKind = result switch
        {
            ExecuteCommandResultType.Success => ResourceCommandResponseKind.Succeeded,
            ExecuteCommandResultType.Canceled => ResourceCommandResponseKind.Cancelled,
            ExecuteCommandResultType.Failure => ResourceCommandResponseKind.Failed,
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
