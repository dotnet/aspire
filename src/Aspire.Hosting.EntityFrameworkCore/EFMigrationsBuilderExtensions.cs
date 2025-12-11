// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding EF Core migration management to projects.
/// </summary>
public static class EFMigrationsBuilderExtensions
{
    /// <summary>
    /// Adds EF Core migration management for a specific DbContext type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type to manage migrations for.</typeparam>
    /// <param name="builder">The resource builder for the project.</param>
    /// <param name="name">The name of the migration resource.</param>
    /// <returns>An EF migration resource builder for chaining additional configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if migrations for this context type have already been added.</exception>
    /// <remarks>
    /// Multiple calls to this method with different context types are supported, allowing you to manage
    /// migrations for multiple DbContexts in the same project.
    /// </remarks>
    public static EFMigrationResourceBuilder AddEFMigrations<TContext>(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name) where TContext : class
    {
        return builder.AddEFMigrations(name, typeof(TContext));
    }

    /// <summary>
    /// Adds EF Core migration management for a specific DbContext type.
    /// </summary>
    /// <param name="builder">The resource builder for the project.</param>
    /// <param name="name">The name of the migration resource.</param>
    /// <param name="contextType">The DbContext type to manage migrations for.</param>
    /// <returns>An EF migration resource builder for chaining additional configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if migrations for this context type have already been added.</exception>
    /// <remarks>
    /// Multiple calls to this method with different context types are supported, allowing you to manage
    /// migrations for multiple DbContexts in the same project.
    /// </remarks>
    public static EFMigrationResourceBuilder AddEFMigrations(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name,
        Type contextType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contextType);

        return AddEFMigrationsCore(builder, name, contextType.FullName);
    }

    /// <summary>
    /// Adds EF Core migration management for a specific DbContext type identified by name.
    /// </summary>
    /// <param name="builder">The resource builder for the project.</param>
    /// <param name="name">The name of the migration resource.</param>
    /// <param name="contextTypeName">The fully qualified name of the DbContext type to manage migrations for.</param>
    /// <returns>An EF migration resource builder for chaining additional configuration.</returns>
    /// <exception cref="InvalidOperationException">Thrown if migrations for this context type have already been added.</exception>
    /// <remarks>
    /// <para>
    /// Multiple calls to this method with different context types are supported, allowing you to manage
    /// migrations for multiple DbContexts in the same project.
    /// </para>
    /// <para>
    /// This overload is useful when the DbContext type is not available at compile time, such as when
    /// using runtime-discovered context types.
    /// </para>
    /// </remarks>
    public static EFMigrationResourceBuilder AddEFMigrations(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name,
        string contextTypeName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(contextTypeName);

        return AddEFMigrationsCore(builder, name, contextTypeName);
    }

    /// <summary>
    /// Adds EF Core migration management for auto-detected DbContext types.
    /// </summary>
    /// <param name="builder">The resource builder for the project.</param>
    /// <param name="name">The name of the migration resource.</param>
    /// <returns>An EF migration resource builder for chaining additional configuration.</returns>
    public static EFMigrationResourceBuilder AddEFMigrations(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        return AddEFMigrationsCore(builder, name, contextTypeName: null);
    }

    private static EFMigrationResourceBuilder AddEFMigrationsCore(
        IResourceBuilder<ProjectResource> builder,
        string name,
        string? contextTypeName)
    {
        // Register the event subscriber once
        EnsureEventSubscriberRegistered(builder.ApplicationBuilder);

        // Check for duplicate context types
        if (contextTypeName != null)
        {
            var existingMigrations = builder.ApplicationBuilder.Resources
                .OfType<EFMigrationResource>()
                .Where(r => r.ProjectResource == builder.Resource && r.ContextTypeName == contextTypeName);

            if (existingMigrations.Any())
            {
                var shortName = contextTypeName.Contains('.') 
                    ? contextTypeName.Substring(contextTypeName.LastIndexOf('.') + 1)
                    : contextTypeName;
                throw new InvalidOperationException(
                    $"The DbContext type '{shortName}' has already been registered for EF migrations on resource '{builder.Resource.Name}'.");
            }
        }

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextTypeName);

        var innerBuilder = builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .AddEFMigrationCommands(contextTypeName);

        return new EFMigrationResourceBuilder(innerBuilder);
    }

    private static void EnsureEventSubscriberRegistered(IDistributedApplicationBuilder applicationBuilder)
    {
        // TryAddEventingSubscriber uses TryAddEnumerable internally, which ensures that even if this method
        // is called multiple times (e.g., when AddEFMigrations is called for multiple DbContexts),
        // the EFMigrationEventSubscriber is only registered once in the DI container.
        applicationBuilder.Services.TryAddEventingSubscriber<EFMigrationEventSubscriber>();
    }

    private static IResourceBuilder<EFMigrationResource> AddEFMigrationCommands(
        this IResourceBuilder<EFMigrationResource> builder,
        string? contextTypeName)
    {
        // Get short name from fully qualified name for display purposes
        string? contextShortName = null;
        if (!string.IsNullOrEmpty(contextTypeName))
        {
            var lastDotIndex = contextTypeName.LastIndexOf('.');
            contextShortName = lastDotIndex >= 0 ? contextTypeName.Substring(lastDotIndex + 1) : contextTypeName;
        }

        // Command names must be valid identifiers (no spaces/parentheses)
        var contextNameSuffix = contextShortName != null ? $"-{contextShortName}" : "";
        // Display names can have friendly formatting
        var contextDisplaySuffix = contextShortName != null ? $" ({contextShortName})" : "";

        // Update Database command
        builder.WithCommand(
            name: $"ef-database-update{contextNameSuffix}",
            displayName: $"Update Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context, 
                "Update Database", 
                contextTypeName,
                async executor => await executor.UpdateDatabaseAsync().ConfigureAwait(false)),
            commandOptions: new CommandOptions
            {
                Description = "Apply pending migrations to the database",
                IconName = "ArrowSync",
                IconVariant = IconVariant.Regular
            });

        // Drop Database command
        builder.WithCommand(
            name: $"ef-database-drop{contextNameSuffix}",
            displayName: $"Drop Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context, 
                "Drop Database", 
                contextTypeName,
                async executor => await executor.DropDatabaseAsync().ConfigureAwait(false)),
            commandOptions: new CommandOptions
            {
                Description = "Delete the database",
                IconName = "Delete",
                IconVariant = IconVariant.Regular,
                ConfirmationMessage = "Are you sure you want to drop the database? This action cannot be undone."
            });

        // Reset Database command
        builder.WithCommand(
            name: $"ef-database-reset{contextNameSuffix}",
            displayName: $"Reset Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context, 
                "Reset Database", 
                contextTypeName,
                async executor => await executor.ResetDatabaseAsync().ConfigureAwait(false)),
            commandOptions: new CommandOptions
            {
                Description = "Drop and recreate the database with all migrations applied",
                IconName = "ArrowReset",
                IconVariant = IconVariant.Regular,
                ConfirmationMessage = "Are you sure you want to reset the database? This will delete all data and cannot be undone."
            });

        // Add Migration command
        builder.WithCommand(
            name: $"ef-migrations-add{contextNameSuffix}",
            displayName: $"Add Migration...{contextDisplaySuffix}",
            executeCommand: context => ExecuteAddMigrationCommandAsync(context, contextTypeName),
            commandOptions: new CommandOptions
            {
                Description = "Create a new migration. Note: The target project will need to be recompiled after adding a migration.",
                IconName = "Add",
                IconVariant = IconVariant.Regular
            });

        // Remove Migration command
        builder.WithCommand(
            name: $"ef-migrations-remove{contextNameSuffix}",
            displayName: $"Remove Migration{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context, 
                "Remove Migration", 
                contextTypeName,
                async executor => await executor.RemoveMigrationAsync().ConfigureAwait(false)),
            commandOptions: new CommandOptions
            {
                Description = "Remove the last migration",
                IconName = "Subtract",
                IconVariant = IconVariant.Regular
            });

        // Get Database Status command
        builder.WithCommand(
            name: $"ef-database-status{contextNameSuffix}",
            displayName: $"Get Database Status{contextDisplaySuffix}",
            executeCommand: context => ExecuteGetStatusCommandAsync(context, contextTypeName),
            commandOptions: new CommandOptions
            {
                Description = "Show the current migration status of the database",
                IconName = "Info",
                IconVariant = IconVariant.Regular
            });

        return builder;
    }

    private static EFMigrationResource? FindMigrationResource(
        DistributedApplicationModel appModel,
        string resourceName,
        string? contextTypeName)
    {
        return appModel.Resources
            .OfType<EFMigrationResource>()
            .FirstOrDefault(r => r.Name == resourceName && r.ContextTypeName == contextTypeName);
    }

    private static async Task<ExecuteCommandResult> ExecuteEFCommandAsync(
        ExecuteCommandContext context,
        string operationDisplayName,
        string? contextTypeName,
        Func<EFCoreOperationExecutor, Task<EFOperationResult>> executeOperation)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();

        var migrationResource = FindMigrationResource(appModel, context.ResourceName, contextTypeName);

        if (migrationResource == null)
        {
            return CommandResults.Failure($"Could not find EF migration resource '{context.ResourceName}'.");
        }

        var logger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            logger.LogInformation("Executing EF Core {Operation} command for context {ContextType}...", 
                operationDisplayName, contextTypeName ?? "(auto-detect)");

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                contextTypeName,
                logger,
                context.CancellationToken);

            var result = await executeOperation(executor).ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogInformation("EF Core {Operation} command completed successfully.", operationDisplayName);
                return CommandResults.Success();
            }
            else
            {
                logger.LogError("EF Core {Operation} command failed: {Error}", operationDisplayName, result.ErrorMessage);
                return CommandResults.Failure(result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("EF Core {Operation} command was cancelled.", operationDisplayName);
            return CommandResults.Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EF Core {Operation} command failed with exception.", operationDisplayName);
            return CommandResults.Failure(ex);
        }
    }

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only
    private static async Task<ExecuteCommandResult> ExecuteAddMigrationCommandAsync(
        ExecuteCommandContext context,
        string? contextTypeName)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();

        var migrationResource = FindMigrationResource(appModel, context.ResourceName, contextTypeName);

        if (migrationResource == null)
        {
            return CommandResults.Failure($"Could not find EF migration resource '{context.ResourceName}'.");
        }

        var logger = resourceLoggerService.GetLogger(migrationResource);

        // Prompt for migration name using IInteractionService
        string? migrationName;
        if (interactionService == null || !interactionService.IsAvailable)
        {
            // Fall back to auto-generated name if interaction service is not available
            migrationName = $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
        else
        {
            var inputResult = await interactionService.PromptInputAsync(
                title: "Add Migration",
                message: "Enter the name for the new migration.",
                inputLabel: "Migration Name",
                placeHolder: "e.g. InitialCreate",
                cancellationToken: context.CancellationToken).ConfigureAwait(false);

            if (inputResult.Canceled || string.IsNullOrWhiteSpace(inputResult.Data?.Value))
            {
                logger.LogInformation("Add migration cancelled by user.");
                return CommandResults.Canceled();
            }

            migrationName = inputResult.Data.Value;
        }

        try
        {
            logger.LogInformation("Creating migration '{MigrationName}' for context {ContextType}...", 
                migrationName, contextTypeName ?? "(auto-detect)");

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                contextTypeName,
                logger,
                context.CancellationToken);

            // Pass configured output directory and namespace from options
            var result = await executor.AddMigrationAsync(
                migrationName, 
                migrationResource.Options.MigrationOutputDirectory,
                migrationResource.Options.MigrationNamespace).ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogInformation("Migration '{MigrationName}' created successfully.", migrationName);

                // Show notification about recompilation requirement
                if (interactionService != null && interactionService.IsAvailable)
                {
                    await interactionService.PromptNotificationAsync(
                        title: "Migration Created",
                        message: $"Migration '{migrationName}' was added successfully.\n\nThe target project needs to be recompiled before the migration can be applied.",
                        options: new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Warning,
                            ShowSecondaryButton = false
                        },
                        cancellationToken: context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogWarning("Migration '{MigrationName}' was added successfully. The target project needs to be recompiled before the migration can be applied.", migrationName);
                }

                return CommandResults.Success();
            }
            else
            {
                logger.LogError("Add Migration command failed: {Error}", result.ErrorMessage);
                return CommandResults.Failure(result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Add Migration command was cancelled.");
            return CommandResults.Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Add Migration command failed with exception.");
            return CommandResults.Failure(ex);
        }
    }

    private static async Task<ExecuteCommandResult> ExecuteGetStatusCommandAsync(
        ExecuteCommandContext context,
        string? contextTypeName)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();

        var migrationResource = FindMigrationResource(appModel, context.ResourceName, contextTypeName);

        if (migrationResource == null)
        {
            return CommandResults.Failure($"Could not find EF migration resource '{context.ResourceName}'.");
        }

        var logger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            logger.LogInformation("Getting database status for context {ContextType}...", 
                contextTypeName ?? "(auto-detect)");

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                contextTypeName,
                logger,
                context.CancellationToken);

            var result = await executor.GetDatabaseStatusAsync().ConfigureAwait(false);

            if (result.Success)
            {
                // Show status in a message box if interaction service is available
                if (interactionService != null && interactionService.IsAvailable)
                {
                    await interactionService.PromptMessageBoxAsync(
                        title: "Database Migration Status",
                        message: result.Output ?? "No migration information available.",
                        options: new MessageBoxInteractionOptions
                        {
                            Intent = MessageIntent.Information,
                            ShowSecondaryButton = false
                        },
                        cancellationToken: context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogInformation("Database status:\n{Status}", result.Output);
                }

                return CommandResults.Success();
            }
            else
            {
                logger.LogError("Get Database Status command failed: {Error}", result.ErrorMessage);
                return CommandResults.Failure(result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Get Database Status command was cancelled.");
            return CommandResults.Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get Database Status command failed with exception.");
            return CommandResults.Failure(ex);
        }
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only
}
