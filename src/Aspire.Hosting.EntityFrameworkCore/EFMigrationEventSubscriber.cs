// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Event subscriber that handles EF migration operations on startup.
/// </summary>
internal sealed class EFMigrationEventSubscriber(
    ResourceNotificationService resourceNotificationService,
    ResourceLoggerService resourceLoggerService,
    ILogger<EFMigrationEventSubscriber> logger) : IDistributedApplicationEventingSubscriber
{

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (executionContext.IsRunMode)
        {
            // In run mode, subscribe to AfterResourcesCreatedEvent to discover migration resources,
            // then subscribe to BeforeResourceStartedEvent for each one to apply migrations
            eventing.Subscribe<AfterResourcesCreatedEvent>((e, ct) => OnAfterResourcesCreatedAsync(e, eventing, executionContext, ct));
        }

        return Task.CompletedTask;
    }

    private Task OnAfterResourcesCreatedAsync(AfterResourcesCreatedEvent @event, IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken _)
    {
        var migrationResources = @event.Model.Resources
            .OfType<EFMigrationResource>()
            .Where(r => r.RunDatabaseUpdateOnStart)
            .ToList();

        if (migrationResources.Count == 0)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Found {Count} EF migration resource(s) configured to run on startup.", migrationResources.Count);

        // Subscribe to BeforeResourceStartedEvent for each migration resource
        // This way migrations run when the resource is being started, avoiding deadlocks
        foreach (var migrationResource in migrationResources)
        {
            eventing.Subscribe<BeforeResourceStartedEvent>(migrationResource, async (e, ct) =>
            {
                await ApplyMigrationsAsync(migrationResource, executionContext, ct).ConfigureAwait(false);
            });
        }

        return Task.CompletedTask;
    }

    private async Task ApplyMigrationsAsync(EFMigrationResource migrationResource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var resourceLogger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            // Update state to Running
            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);

            resourceLogger.LogInformation("Starting database migration for '{ResourceName}'...", migrationResource.Name);

            await EFResourceBuilderExtensions.ProcessEnvironmentVariablesAsync(migrationResource, executionContext, cancellationToken).ConfigureAwait(false);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.MigrationsProject,
                migrationResource.ContextTypeName,
                resourceLogger,
                cancellationToken);

            var result = await executor.UpdateDatabaseAsync().ConfigureAwait(false);

            if (result.Success)
            {
                resourceLogger.LogInformation("Database migration completed successfully for '{ResourceName}'.", migrationResource.Name);

                // Update state to Active
                await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.Active, KnownResourceStateStyles.Info)
                }).ConfigureAwait(false);
            }
            else
            {
                resourceLogger.LogError("Database migration failed for '{ResourceName}': {Error}", migrationResource.Name, result.ErrorMessage);

                // Update state to FailedToStart
                await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
                {
                    State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            resourceLogger.LogWarning("Database migration was cancelled for '{ResourceName}'.", migrationResource.Name);

            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            resourceLogger.LogError(ex, "Database migration failed with exception for '{ResourceName}'.", migrationResource.Name);

            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
            }).ConfigureAwait(false);
        }
    }
}
