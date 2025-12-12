// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Event subscriber that handles EF migration operations on startup and during publishing.
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
            // In run mode, apply migrations on startup
            eventing.Subscribe<AfterResourcesCreatedEvent>(OnAfterResourcesCreatedAsync);
        }
        else if (executionContext.IsPublishMode)
        {
            // In publish mode, generate migration scripts/bundles
            eventing.Subscribe<BeforePublishEvent>(OnBeforePublishAsync);
        }

        return Task.CompletedTask;
    }

    private async Task OnBeforePublishAsync(BeforePublishEvent @event, CancellationToken cancellationToken)
    {
        var migrationResources = @event.Model.Resources
            .OfType<EFMigrationResource>()
            .Where(r => r.Options.PublishAsMigrationScript || r.Options.PublishAsMigrationBundle)
            .ToList();

        if (migrationResources.Count == 0)
        {
            return;
        }

        logger.LogInformation("Found {Count} EF migration resource(s) configured for publish.", migrationResources.Count);

        foreach (var migrationResource in migrationResources)
        {
            await GeneratePublishArtifactsAsync(migrationResource, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task GeneratePublishArtifactsAsync(EFMigrationResource migrationResource, CancellationToken cancellationToken)
    {
        var resourceLogger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.Options.MigrationsProject,
                migrationResource.ContextTypeName,
                resourceLogger,
                cancellationToken);

            if (migrationResource.Options.PublishAsMigrationScript)
            {
                resourceLogger.LogInformation("Generating migration script for '{ResourceName}'...", migrationResource.Name);
                var result = await executor.GenerateMigrationScriptAsync().ConfigureAwait(false);

                if (result.Success)
                {
                    resourceLogger.LogInformation("Migration script generated successfully for '{ResourceName}'.", migrationResource.Name);
                }
                else
                {
                    resourceLogger.LogWarning("Failed to generate migration script for '{ResourceName}': {Error}", 
                        migrationResource.Name, result.ErrorMessage);
                }
            }

            if (migrationResource.Options.PublishAsMigrationBundle)
            {
                resourceLogger.LogInformation("Generating migration bundle for '{ResourceName}'...", migrationResource.Name);
                var result = await executor.GenerateMigrationBundleAsync().ConfigureAwait(false);

                if (result.Success)
                {
                    resourceLogger.LogInformation("Migration bundle generated successfully for '{ResourceName}'.", migrationResource.Name);
                }
                else
                {
                    resourceLogger.LogWarning("Failed to generate migration bundle for '{ResourceName}': {Error}", 
                        migrationResource.Name, result.ErrorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            resourceLogger.LogError(ex, "Failed to generate publish artifacts for '{ResourceName}'.", migrationResource.Name);
        }
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

        // Run migrations sequentially to avoid concurrency issues
        foreach (var migrationResource in migrationResources)
        {
            await ApplyMigrationsAsync(migrationResource, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ApplyMigrationsAsync(EFMigrationResource migrationResource, CancellationToken cancellationToken)
    {
        var resourceLogger = resourceLoggerService.GetLogger(migrationResource);
        
        try
        {
            // Wait for the project resource (containing the DB connection) to be healthy before applying migrations
            resourceLogger.LogInformation("Waiting for '{ProjectName}' to be healthy before applying migrations...", 
                migrationResource.ProjectResource.Name);
            
            await resourceNotificationService.WaitForResourceHealthyAsync(
                migrationResource.ProjectResource.Name,
                cancellationToken).ConfigureAwait(false);

            // Update state to Running
            await resourceNotificationService.PublishUpdateAsync(migrationResource, state => state with
            {
                State = new ResourceStateSnapshot("Running", KnownResourceStateStyles.Info)
            }).ConfigureAwait(false);

            resourceLogger.LogInformation("Starting database migration for '{ResourceName}'...", migrationResource.Name);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.Options.MigrationsProject,
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
