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

        return AddEFMigrationsCore(builder, name, contextType, contextType.FullName);
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

        // Register the event subscriber once
        EnsureEventSubscriberRegistered(builder.ApplicationBuilder);

        // Check for duplicate context types by name
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

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextType: null, contextTypeName);

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

        return new EFMigrationResourceBuilder(innerBuilder, contextTypeName);
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

        // Register the event subscriber once
        EnsureEventSubscriberRegistered(builder.ApplicationBuilder);

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextType: null, contextTypeName: null);

        var innerBuilder = builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .AddEFMigrationCommands(contextTypeName: null);

        return new EFMigrationResourceBuilder(innerBuilder, contextTypeName: null);
    }

    private static EFMigrationResourceBuilder AddEFMigrationsCore(
        IResourceBuilder<ProjectResource> builder,
        string name,
        Type? contextType,
        string? contextTypeName)
    {
        // Register the event subscriber once
        EnsureEventSubscriberRegistered(builder.ApplicationBuilder);

        // Check for duplicate context types
        if (contextType != null)
        {
            var existingMigrations = builder.ApplicationBuilder.Resources
                .OfType<EFMigrationResource>()
                .Where(r => r.ProjectResource == builder.Resource && r.ContextType == contextType);

            if (existingMigrations.Any())
            {
                throw new InvalidOperationException(
                    $"The DbContext type '{contextType.Name}' has already been registered for EF migrations on resource '{builder.Resource.Name}'.");
            }
        }

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextType, contextTypeName);

        var innerBuilder = builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .AddEFMigrationCommands(contextTypeName ?? contextType?.FullName);

        return new EFMigrationResourceBuilder(innerBuilder, contextTypeName ?? contextType?.FullName);
    }

    private static void EnsureEventSubscriberRegistered(IDistributedApplicationBuilder applicationBuilder)
    {
        // TryAddEventingSubscriber uses TryAddEnumerable which handles the "already registered" check automatically
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "update", contextTypeName).ConfigureAwait(false),
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "drop", contextTypeName).ConfigureAwait(false),
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "reset", contextTypeName).ConfigureAwait(false),
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "add-migration", contextTypeName).ConfigureAwait(false),
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "remove-migration", contextTypeName).ConfigureAwait(false),
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
            executeCommand: async context => await ExecuteEFCommandAsync(context, "status", contextTypeName).ConfigureAwait(false),
            commandOptions: new CommandOptions
            {
                Description = "Show the current migration status of the database",
                IconName = "Info",
                IconVariant = IconVariant.Regular
            });

        return builder;
    }

    private static async Task<ExecuteCommandResult> ExecuteEFCommandAsync(
        ExecuteCommandContext context,
        string operation,
        string? contextTypeName)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var appModel = context.ServiceProvider.GetRequiredService<DistributedApplicationModel>();

        var migrationResource = appModel.Resources
            .OfType<EFMigrationResource>()
            .FirstOrDefault(r => r.Name == context.ResourceName);

        if (migrationResource == null)
        {
            return CommandResults.Failure($"Could not find EF migration resource '{context.ResourceName}'.");
        }

        var logger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            logger.LogInformation("Executing EF Core {Operation} command for context {ContextType}...", 
                operation, contextTypeName ?? "(auto-detect)");

            var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                contextTypeName,
                logger,
                context.CancellationToken);

            EFOperationResult result;
            
            if (operation == "add-migration")
            {
                // Prompt for migration name using IInteractionService
                var migrationName = await PromptForMigrationNameAsync(context).ConfigureAwait(false);
                if (migrationName == null)
                {
                    logger.LogInformation("Add migration cancelled by user.");
                    return CommandResults.Canceled();
                }
                result = await executor.AddMigrationAsync(migrationName).ConfigureAwait(false);
                
                // Notify user about recompilation requirement after successful migration addition
                if (result.Success)
                {
                    logger.LogWarning("Migration '{MigrationName}' was added successfully. The target project needs to be recompiled before the migration can be applied.", migrationName);
                }
            }
            else
            {
                result = operation switch
                {
                    "update" => await executor.UpdateDatabaseAsync().ConfigureAwait(false),
                    "drop" => await executor.DropDatabaseAsync().ConfigureAwait(false),
                    "reset" => await executor.ResetDatabaseAsync().ConfigureAwait(false),
                    "remove-migration" => await executor.RemoveMigrationAsync().ConfigureAwait(false),
                    "status" => await executor.GetDatabaseStatusAsync().ConfigureAwait(false),
                    _ => throw new InvalidOperationException($"Unknown operation: {operation}")
                };
            }

            if (result.Success)
            {
                logger.LogInformation("EF Core {Operation} command completed successfully.", operation);
                return CommandResults.Success();
            }
            else
            {
                logger.LogError("EF Core {Operation} command failed: {Error}", operation, result.ErrorMessage);
                return CommandResults.Failure(result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("EF Core {Operation} command was cancelled.", operation);
            return CommandResults.Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EF Core {Operation} command failed with exception.", operation);
            return CommandResults.Failure(ex);
        }
    }

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only
    private static async Task<string?> PromptForMigrationNameAsync(ExecuteCommandContext context)
    {
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();
        
        if (interactionService == null || !interactionService.IsAvailable)
        {
            // Fall back to auto-generated name if interaction service is not available
            return $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        var result = await interactionService.PromptInputAsync(
            title: "Add Migration",
            message: "Enter the name for the new migration. Note: The target project will need to be recompiled after adding a migration.",
            inputLabel: "Migration Name",
            placeHolder: "e.g. InitialCreate",
            cancellationToken: context.CancellationToken).ConfigureAwait(false);

        if (result.Canceled || string.IsNullOrWhiteSpace(result.Data?.Value))
        {
            return null;
        }

        return result.Data.Value;
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only
}
