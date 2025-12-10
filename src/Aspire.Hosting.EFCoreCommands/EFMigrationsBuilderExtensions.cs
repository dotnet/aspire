// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
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
    /// <returns>A resource builder for the EF migration resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if migrations for this context type have already been added.</exception>
    public static IResourceBuilder<EFMigrationResource> AddEFMigrations<TContext>(
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
    /// <returns>A resource builder for the EF migration resource.</returns>
    /// <exception cref="InvalidOperationException">Thrown if migrations for this context type have already been added.</exception>
    public static IResourceBuilder<EFMigrationResource> AddEFMigrations(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name,
        Type contextType)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contextType);

        // Check for duplicate context types
        var existingMigrations = builder.ApplicationBuilder.Resources
            .OfType<EFMigrationResource>()
            .Where(r => r.ProjectResource == builder.Resource && r.ContextType == contextType);

        if (existingMigrations.Any())
        {
            throw new InvalidOperationException(
                $"EF migrations for context type '{contextType.FullName}' have already been added to project '{builder.Resource.Name}'.");
        }

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextType);

        return builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .AddEFMigrationCommands(contextType);
    }

    /// <summary>
    /// Adds EF Core migration management for auto-detected DbContext types.
    /// </summary>
    /// <param name="builder">The resource builder for the project.</param>
    /// <param name="name">The name of the migration resource.</param>
    /// <returns>A resource builder for the EF migration resource.</returns>
    public static IResourceBuilder<EFMigrationResource> AddEFMigrations(
        this IResourceBuilder<ProjectResource> builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextType: null);

        return builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot("Pending", KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .AddEFMigrationCommands(contextType: null);
    }

    /// <summary>
    /// Configures the EF migration resource to run database update when the AppHost starts.
    /// </summary>
    /// <param name="builder">The resource builder for the EF migration resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<EFMigrationResource> RunDatabaseUpdateOnStart(
        this IResourceBuilder<EFMigrationResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new RunDatabaseUpdateOnStartAnnotation());
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration script during publishing.
    /// </summary>
    /// <param name="builder">The resource builder for the EF migration resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<EFMigrationResource> PublishAsMigrationScript(
        this IResourceBuilder<EFMigrationResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new PublishAsMigrationScriptAnnotation());
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration bundle during publishing.
    /// </summary>
    /// <param name="builder">The resource builder for the EF migration resource.</param>
    /// <returns>The resource builder for chaining.</returns>
    public static IResourceBuilder<EFMigrationResource> PublishAsMigrationBundle(
        this IResourceBuilder<EFMigrationResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new PublishAsMigrationBundleAnnotation());
    }

    private static IResourceBuilder<EFMigrationResource> AddEFMigrationCommands(
        this IResourceBuilder<EFMigrationResource> builder,
        Type? contextType)
    {
        var contextTypeName = contextType?.FullName;
        // Command names must be valid identifiers (no spaces/parentheses)
        var contextNameSuffix = contextType != null ? $"-{contextType.Name}" : "";
        // Display names can have friendly formatting
        var contextDisplaySuffix = contextType != null ? $" ({contextType.Name})" : "";

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

            var result = operation switch
            {
                "update" => await executor.UpdateDatabaseAsync().ConfigureAwait(false),
                "drop" => await executor.DropDatabaseAsync().ConfigureAwait(false),
                "reset" => await executor.ResetDatabaseAsync().ConfigureAwait(false),
                "add-migration" => await executor.AddMigrationAsync().ConfigureAwait(false),
                "remove-migration" => await executor.RemoveMigrationAsync().ConfigureAwait(false),
                "status" => await executor.GetDatabaseStatusAsync().ConfigureAwait(false),
                _ => throw new InvalidOperationException($"Unknown operation: {operation}")
            };

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
}
