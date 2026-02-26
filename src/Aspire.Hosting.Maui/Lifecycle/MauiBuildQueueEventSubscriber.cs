// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui.Annotations;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui.Lifecycle;

/// <summary>
/// Event subscriber that serializes MAUI platform resource builds per-project.
/// </summary>
/// <remarks>
/// Multiple MAUI platform resources (Android, iOS, Mac Catalyst, Windows) can reference
/// the same project. MSBuild cannot handle concurrent builds of the same project file,
/// so this subscriber uses a semaphore to ensure only one platform builds at a time.
/// Resources waiting for their turn show a "Queued" state in the dashboard.
/// </remarks>
internal sealed class MauiBuildQueueEventSubscriber(
    ResourceNotificationService notificationService,
    ResourceLoggerService loggerService) : IDistributedApplicationEventingSubscriber
{
    private static readonly ResourceStateSnapshot s_queuedState = new("Queued", KnownResourceStateStyles.Info);
    private static readonly ResourceStateSnapshot s_buildingState = new("Building", KnownResourceStateStyles.Info);
    private CancellationToken _appLifetimeToken;

    /// <inheritdoc/>
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        _appLifetimeToken = cancellationToken;
        eventing.Subscribe<BeforeResourceStartedEvent>(OnBeforeResourceStartedAsync);
        return Task.CompletedTask;
    }

    private async Task OnBeforeResourceStartedAsync(BeforeResourceStartedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.Resource is not IMauiPlatformResource mauiResource)
        {
            return;
        }

        var resource = @event.Resource;
        var parent = mauiResource.Parent;
        var logger = loggerService.GetLogger(resource);

        if (!parent.TryGetLastAnnotation<MauiBuildQueueAnnotation>(out var queueAnnotation))
        {
            // Annotation is added eagerly in AddMauiProject — should always be present.
            return;
        }

        var semaphore = queueAnnotation.BuildSemaphore;

        // If the semaphore is already held, show "Queued" state while waiting.
        if (semaphore.CurrentCount == 0)
        {
            logger.LogInformation("Queued — waiting for another build of project '{ProjectName}' to complete.", parent.Name);

            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = s_queuedState
            }).ConfigureAwait(false);
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Building project '{ProjectName}' for {ResourceName}.", parent.Name, resource.Name);

        await notificationService.PublishUpdateAsync(resource, s => s with
        {
            State = s_buildingState
        }).ConfigureAwait(false);

        // Fire-and-forget: release the semaphore when the resource reaches Running or a terminal state.
        // We release on Running (not just terminal states) because some platforms like iOS and Mac Catalyst
        // stay in Running state indefinitely — we only need to hold the lock during the build phase.
        _ = ReleaseSemaphoreOnCompletionAsync(resource, semaphore, logger, _appLifetimeToken);
    }

    private async Task ReleaseSemaphoreOnCompletionAsync(
        IResource resource,
        SemaphoreSlim semaphore,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Release when the resource transitions to Running (build completed, app launched)
            // or a terminal state (build failed or process exited).
            await notificationService.WaitForResourceAsync(
                resource.Name,
                re => re.Snapshot.State?.Text == KnownResourceStates.Running
                    || KnownResourceStates.TerminalStates.Contains(re.Snapshot.State?.Text),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // AppHost shutting down or resource cancelled — release the lock.
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error waiting for resource '{ResourceName}' to complete.", resource.Name);
        }
        finally
        {
            semaphore.Release();
            logger.LogDebug("Released build lock for project '{ProjectName}' (resource '{ResourceName}').", resource.Name, resource.Name);
        }
    }
}
