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
        // Only subscribe in run mode, not publish mode
        if (!executionContext.IsRunMode)
        {
            return Task.CompletedTask;
        }

        eventing.Subscribe<AfterResourcesCreatedEvent>(OnAfterResourcesCreatedAsync);

        return Task.CompletedTask;
    }

    private async Task OnAfterResourcesCreatedAsync(AfterResourcesCreatedEvent @event, CancellationToken cancellationToken)
    {
        var migrationResources = @event.Model.Resources
            .OfType<EFMigrationResource>()
            .Where(r => r.Options.RunDatabaseUpdateOnStart)
            .ToList();

        if (migrationResources.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} EF migration resource(s) configured to run on startup.", migrationResources.Count);

        // Run migrations in parallel for each resource
        var migrationTasks = migrationResources.Select(r => RunMigrationsAsync(r, cancellationToken));
        await Task.WhenAll(migrationTasks).ConfigureAwait(false);
    }

    private async Task RunMigrationsAsync(EFMigrationResource migrationResource, CancellationToken cancellationToken)
    {
        var resourceLogger = resourceLoggerService.GetLogger(migrationResource);
        
        try
        {
            // Update state to Running
            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);

            resourceLogger.LogInformation("Starting database migration for '{ResourceName}'...", migrationResource.Name);

            var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.ContextTypeName,
                resourceLogger,
                cancellationToken);

            var result = await executor.UpdateDatabaseAsync().ConfigureAwait(false);

            if (result.Success)
            {
                resourceLogger.LogInformation("Database migration completed successfully for '{ResourceName}'.", migrationResource.Name);
                
                // Update state to Finished
                await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
                {
                    State = new ResourceStateSnapshot("Finished", KnownResourceStateStyles.Success)
                }).ConfigureAwait(false);
            }
            else
            {
                resourceLogger.LogError("Database migration failed for '{ResourceName}': {Error}", migrationResource.Name, result.ErrorMessage);
                
                // Update state to FailedToStart
                await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
                {
                    State = new ResourceStateSnapshot("FailedToStart", KnownResourceStateStyles.Error)
                }).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            resourceLogger.LogWarning("Database migration was cancelled for '{ResourceName}'.", migrationResource.Name);
            
            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot("Stopped", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            resourceLogger.LogError(ex, "Database migration failed with exception for '{ResourceName}'.", migrationResource.Name);
            
            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot("FailedToStart", KnownResourceStateStyles.Error)
            }).ConfigureAwait(false);
        }
    }
}
