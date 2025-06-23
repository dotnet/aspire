// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.ResourceService.Proto.V1;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static Aspire.Hosting.ApplicationModel.Interaction;

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

    public override async Task WatchInteractions(IAsyncStreamReader<WatchInteractionsRequestUpdate> requestStream, IServerStreamWriter<WatchInteractionsResponseUpdate> responseStream, ServerCallContext context)
    {
        await ExecuteAsync(
            WatchInteractionsInternal,
            context).ConfigureAwait(false);

        async Task WatchInteractionsInternal(CancellationToken cancellationToken)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var updates = serviceData.SubscribeInteractionUpdates();

            // Send
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var interaction in updates.WithCancellation(cts.Token).ConfigureAwait(false))
                    {
                        var change = new WatchInteractionsResponseUpdate();
                        change.InteractionId = interaction.InteractionId;
                        change.Title = interaction.Title;
                        if (interaction.Message != null)
                        {
                            change.Message = interaction.Message;
                        }
                        if (interaction.Options.PrimaryButtonText != null)
                        {
                            change.PrimaryButtonText = interaction.Options.PrimaryButtonText;
                        }
                        if (interaction.Options.SecondaryButtonText != null)
                        {
                            change.SecondaryButtonText = interaction.Options.SecondaryButtonText;
                        }
                        change.ShowDismiss = interaction.Options.ShowDismiss;
                        change.ShowSecondaryButton = interaction.Options.ShowSecondaryButton;

                        if (interaction.State == InteractionState.Complete)
                        {
                            change.Complete = new InteractionComplete();
                        }
                        else if (interaction.InteractionInfo is MessageBoxInteractionInfo messageBox)
                        {
                            change.MessageBox = new InteractionMessageBox();
                            change.MessageBox.Intent = MapMessageIntent(messageBox.Intent);
                        }
                        else if (interaction.InteractionInfo is MessageBarInteractionInfo messageBar)
                        {
                            change.MessageBar = new InteractionMessageBar();
                            change.MessageBar.Intent = MapMessageIntent(messageBar.Intent);
                        }
                        else if (interaction.InteractionInfo is InputsInteractionInfo inputs)
                        {
                            change.InputsDialog = new InteractionInputsDialog();

                            var inputInstances = inputs.Inputs.Select(input =>
                            {
                                var dto = new InteractionInput
                                {
                                    InputType = MapInputType(input.InputType),
                                    Required = input.Required
                                };
                                if (input.Label != null)
                                {
                                    dto.Label = input.Label;
                                }
                                if (input.Placeholder != null)
                                {
                                    dto.Placeholder = input.Placeholder;
                                }
                                if (input.Value != null)
                                {
                                    dto.Value = input.Value;
                                }
                                if (input.Options != null)
                                {
                                    dto.Options.Add(input.Options.ToDictionary());
                                }
                                return dto;
                            }).ToList();
                            change.InputsDialog.InputItems.AddRange(inputInstances);
                        }

                        await responseStream.WriteAsync(change, cts.Token).ConfigureAwait(false);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error while watching interactions.");
                }
                finally
                {
                    cts.Cancel();
                }
            }, cts.Token);

            // Receive
            try
            {
                await foreach (var request in requestStream.ReadAllAsync(cts.Token).ConfigureAwait(false))
                {
                    await serviceData.SendInteractionRequestAsync(request).ConfigureAwait(false);
                }
            }
            finally
            {
                // Ensure the write task is cancelled if we exit the loop.
                cts.Cancel();
            }
        }
    }

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    private static MessageIntent MapMessageIntent(ApplicationModel.MessageIntent? intent)
    {
        if (intent is null)
        {
            return MessageIntent.None;
        }

        switch (intent.Value)
        {
            case ApplicationModel.MessageIntent.Success:
                return MessageIntent.Success;
            case ApplicationModel.MessageIntent.Warning:
                return MessageIntent.Warning;
            case ApplicationModel.MessageIntent.Error:
                return MessageIntent.Error;
            case ApplicationModel.MessageIntent.Information:
                return MessageIntent.Information;
            case ApplicationModel.MessageIntent.Confirmation:
                return MessageIntent.Confirmation;
            default:
                return MessageIntent.None;
        }
    }

    private static InputType MapInputType(ApplicationModel.InputType inputType)
    {
        switch (inputType)
        {
            case ApplicationModel.InputType.Text:
                return InputType.Text;
            case ApplicationModel.InputType.Password:
                return InputType.Password;
            case ApplicationModel.InputType.Select:
                return InputType.Select;
            case ApplicationModel.InputType.Checkbox:
                return InputType.Checkbox;
            case ApplicationModel.InputType.Number:
                return InputType.Number;
            default:
                throw new InvalidOperationException($"Unexpected input type: {inputType}");
        }
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
            logger.LogError(ex, "Error executing service method '{Method}'.", serverCallContext.Method);
            throw;
        }
    }
}
