// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding EF Core migration management to projects.
/// </summary>
public static class EFResourceBuilderExtensions
{
    private static string GetShortTypeName(string? fullTypeName)
    {
        if (string.IsNullOrEmpty(fullTypeName))
        {
            return string.Empty;
        }
        var lastDotIndex = fullTypeName.LastIndexOf('.');
        return lastDotIndex >= 0 ? fullTypeName[(lastDotIndex + 1)..] : fullTypeName;
    }

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
        EnsureEventSubscriberRegistered(builder.ApplicationBuilder);

        // Check for duplicate context types and null/non-null conflicts
        var existingMigrations = builder.ApplicationBuilder.Resources
            .OfType<EFMigrationResource>()
            .Where(r => r.ProjectResource == builder.Resource)
            .ToList();

        if (contextTypeName != null)
        {
            if (existingMigrations.Any(r => r.ContextTypeName == contextTypeName))
            {
                throw new InvalidOperationException(
                    $"The DbContext type '{GetShortTypeName(contextTypeName)}' has already been registered for EF migrations on resource '{builder.Resource.Name}'.");
            }

            if (existingMigrations.Any(r => r.ContextTypeName == null))
            {
                throw new InvalidOperationException(
                    $"Cannot add migrations for a specific DbContext type when auto-detected migrations have already been registered on resource '{builder.Resource.Name}'.");
            }
        }
        else
        {
            if (existingMigrations.Any())
            {
                throw new InvalidOperationException(
                    $"Cannot add auto-detected migrations when migrations for specific DbContext types have already been registered on resource '{builder.Resource.Name}'.");
            }
        }

        var migrationResource = new EFMigrationResource(name, builder.Resource, contextTypeName);

        var innerBuilder = builder.ApplicationBuilder
            .AddResource(migrationResource)
            .WithInitialState(new CustomResourceSnapshot
            {
                ResourceType = "EFMigration",
                Properties = [],
                State = new ResourceStateSnapshot(KnownResourceStates.Active, KnownResourceStateStyles.Info)
            })
            .WithIconName("Database")
            .WaitFor(builder)
            .WithPipelineStepFactory(CreateMigrationPipelineStep);

        // Add commands to the original project resource builder
        AddEFMigrationCommands(builder, migrationResource, contextTypeName);

        return new EFMigrationResourceBuilder(innerBuilder);
    }

    private static void EnsureEventSubscriberRegistered(IDistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.TryAddEventingSubscriber<EFMigrationEventSubscriber>();
    }

    private static IEnumerable<PipelineStep> CreateMigrationPipelineStep(PipelineStepFactoryContext context)
    {
        if (context.Resource is not EFMigrationResource migrationResource
            || !migrationResource.PublishAsMigrationScript && !migrationResource.PublishAsMigrationBundle)
        {
            yield break;
        }

        if (migrationResource.PublishAsMigrationScript)
        {
            yield return new PipelineStep
            {
                Name = $"{migrationResource.Name}-generate-migration-script",
                Description = $"Generate EF Core migration SQL script for {migrationResource.Name}",
                Resource = migrationResource,
                Action = async stepContext =>
                {
                    var loggerFactory = stepContext.Services.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<EFMigrationResource>();
                    var executionContext = stepContext.Services.GetRequiredService<DistributedApplicationExecutionContext>();
                    await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, stepContext.CancellationToken).ConfigureAwait(false);

                    using var executor = new EFCoreOperationExecutor(
                        migrationResource.ProjectResource,
                        migrationResource.MigrationsProject,
                        migrationResource.ContextTypeName,
                        logger,
                        stepContext.CancellationToken);

                    logger.LogInformation("Generating migration script for '{ResourceName}'...", migrationResource.Name);
                    var result = await executor.GenerateMigrationScriptAsync().ConfigureAwait(false);

                    if (result.Success)
                    {
                        logger.LogInformation("Migration script generated successfully for '{ResourceName}'.", migrationResource.Name);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to generate migration script for '{migrationResource.Name}': {result.ErrorMessage}");
                    }
                }
            };
        }

        if (migrationResource.PublishAsMigrationBundle)
        {
            yield return new PipelineStep
            {
                Name = $"{migrationResource.Name}-generate-migration-bundle",
                Description = $"Generate EF Core migration bundle for {migrationResource.Name}",
                Resource = migrationResource,
                Action = async stepContext =>
                {
                    var loggerFactory = stepContext.Services.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger<EFMigrationResource>();
                    var executionContext = stepContext.Services.GetRequiredService<DistributedApplicationExecutionContext>();
                    await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, stepContext.CancellationToken).ConfigureAwait(false);

                    using var executor = new EFCoreOperationExecutor(
                        migrationResource.ProjectResource,
                        migrationResource.MigrationsProject,
                        migrationResource.ContextTypeName,
                        logger,
                        stepContext.CancellationToken);

                    logger.LogInformation("Generating migration bundle for '{ResourceName}'...", migrationResource.Name);
                    var result = await executor.GenerateMigrationBundleAsync().ConfigureAwait(false);

                    if (result.Success)
                    {
                        logger.LogInformation("Migration bundle generated successfully for '{ResourceName}'.", migrationResource.Name);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Failed to generate migration bundle for '{migrationResource.Name}': {result.ErrorMessage}");
                    }
                }
            };
        }
    }

    private static void AddEFMigrationCommands(
        IResourceBuilder<ProjectResource> projectBuilder,
        EFMigrationResource migrationResource,
        string? contextTypeName)
    {
        var contextShortName = GetShortTypeName(contextTypeName);
        var contextNameSuffix = !string.IsNullOrEmpty(contextShortName) ? $"-{contextShortName}" : "";
        var contextDisplaySuffix = !string.IsNullOrEmpty(contextShortName) ? $" ({contextShortName})" : "";

        projectBuilder.WithCommand(
            name: $"ef-database-update{contextNameSuffix}",
            displayName: $"Update Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context,
                "Update Database",
                migrationResource,
                executor => executor.UpdateDatabaseAsync()),
            commandOptions: new CommandOptions
            {
                Description = "Apply pending migrations to the database",
                IconName = "ArrowSync",
                IconVariant = IconVariant.Regular,
                UpdateState = _ => migrationResource.RequiresRebuild ? ResourceCommandState.Disabled : ResourceCommandState.Enabled
            });

        projectBuilder.WithCommand(
            name: $"ef-database-drop{contextNameSuffix}",
            displayName: $"Drop Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context,
                "Drop Database",
                migrationResource,
                executor => executor.DropDatabaseAsync()),
            commandOptions: new CommandOptions
            {
                Description = "Delete the database",
                IconName = "Delete",
                IconVariant = IconVariant.Regular,
                ConfirmationMessage = "Are you sure you want to drop the database? This action cannot be undone."
            });

        projectBuilder.WithCommand(
            name: $"ef-database-reset{contextNameSuffix}",
            displayName: $"Reset Database{contextDisplaySuffix}",
            executeCommand: context => ExecuteEFCommandAsync(
                context,
                "Reset Database",
                migrationResource,
                executor => executor.ResetDatabaseAsync()),
            commandOptions: new CommandOptions
            {
                Description = "Drop and recreate the database with all migrations applied",
                IconName = "ArrowReset",
                IconVariant = IconVariant.Regular,
                ConfirmationMessage = "Are you sure you want to reset the database? This will delete all data and cannot be undone."
            });

        projectBuilder.WithCommand(
            name: $"ef-migrations-add{contextNameSuffix}",
            displayName: $"Add Migration...{contextDisplaySuffix}",
            executeCommand: context => ExecuteAddMigrationCommandAsync(context, migrationResource),
            commandOptions: new CommandOptions
            {
                Description = "Create a new migration. Note: The target project will need to be recompiled after adding a migration.",
                IconName = "Add",
                IconVariant = IconVariant.Regular,
                UpdateState = _ => migrationResource.RequiresRebuild ? ResourceCommandState.Disabled : ResourceCommandState.Enabled
            });

        projectBuilder.WithCommand(
            name: $"ef-migrations-remove{contextNameSuffix}",
            displayName: $"Remove Migration{contextDisplaySuffix}",
            executeCommand: context => ExecuteRemoveMigrationCommandAsync(context, migrationResource),
            commandOptions: new CommandOptions
            {
                Description = "Remove the last migration. Note: The target project will need to be recompiled after removing a migration.",
                IconName = "Subtract",
                IconVariant = IconVariant.Regular,
                UpdateState = _ => migrationResource.RequiresRebuild ? ResourceCommandState.Disabled : ResourceCommandState.Enabled
            });

        projectBuilder.WithCommand(
            name: $"ef-database-status{contextNameSuffix}",
            displayName: $"Get Database Status{contextDisplaySuffix}",
            executeCommand: context => ExecuteGetStatusCommandAsync(context, migrationResource),
            commandOptions: new CommandOptions
            {
                Description = "Show the current migration status of the database",
                IconName = "Info",
                IconVariant = IconVariant.Regular,
                UpdateState = _ => migrationResource.RequiresRebuild ? ResourceCommandState.Disabled : ResourceCommandState.Enabled
            });
    }

    private static async Task<ExecuteCommandResult> ExecuteEFCommandAsync(
        ExecuteCommandContext context,
        string operationDisplayName,
        EFMigrationResource migrationResource,
        Func<EFCoreOperationExecutor, Task<EFOperationResult>> executeOperation)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var logger = resourceLoggerService.GetLogger(migrationResource);

        try
        {
            logger.LogInformation("Executing EF Core {Operation} command for context {ContextType}...",
                operationDisplayName, migrationResource.ContextTypeName ?? "(auto-detect)");

            var executionContext = context.ServiceProvider.GetRequiredService<DistributedApplicationExecutionContext>();
            await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, context.CancellationToken).ConfigureAwait(false);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.MigrationsProject,
                migrationResource.ContextTypeName,
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
        EFMigrationResource migrationResource)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();
        var logger = resourceLoggerService.GetLogger(migrationResource);
        var contextTypeName = migrationResource.ContextTypeName;

        string? migrationName;
        if (interactionService == null || !interactionService.IsAvailable)
        {
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
                logger.LogInformation("Add migration command was cancelled.");
                return CommandResults.Canceled();
            }

            migrationName = inputResult.Data.Value;
        }

        try
        {
            logger.LogInformation("Creating migration '{MigrationName}' for context {ContextType}...",
                migrationName, contextTypeName ?? "(auto-detect)");

            var executionContext = context.ServiceProvider.GetRequiredService<DistributedApplicationExecutionContext>();
            await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, context.CancellationToken).ConfigureAwait(false);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.MigrationsProject,
                contextTypeName,
                logger,
                context.CancellationToken);

            var result = await executor.AddMigrationAsync(
                migrationName,
                migrationResource.MigrationOutputDirectory,
                migrationResource.MigrationNamespace).ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogInformation("Migration '{MigrationName}' created successfully.", migrationName);

                // Mark that a rebuild is required
                migrationResource.RequiresRebuild = true;
                logger.LogDebug("Marked migration resource as requiring rebuild. Some commands will be disabled until rebuild.");

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

    private static async Task<ExecuteCommandResult> ExecuteRemoveMigrationCommandAsync(
        ExecuteCommandContext context,
        EFMigrationResource migrationResource)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();
        var logger = resourceLoggerService.GetLogger(migrationResource);
        var contextTypeName = migrationResource.ContextTypeName;

        try
        {
            logger.LogInformation("Removing last migration for context {ContextType}...",
                contextTypeName ?? "(auto-detect)");

            var executionContext = context.ServiceProvider.GetRequiredService<DistributedApplicationExecutionContext>();
            await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, context.CancellationToken).ConfigureAwait(false);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.MigrationsProject,
                contextTypeName,
                logger,
                context.CancellationToken);

            var result = await executor.RemoveMigrationAsync().ConfigureAwait(false);

            if (result.Success)
            {
                logger.LogInformation("Migration removed successfully.");

                if (interactionService != null && interactionService.IsAvailable)
                {
                    await interactionService.PromptNotificationAsync(
                        title: "Migration Removed",
                        message: "The last migration was removed successfully.\n\nThe target project needs to be recompiled.",
                        options: new NotificationInteractionOptions
                        {
                            Intent = MessageIntent.Warning,
                            ShowSecondaryButton = false
                        },
                        cancellationToken: context.CancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogWarning("The last migration was removed successfully. The target project needs to be recompiled.");
                }

                return CommandResults.Success();
            }
            else
            {
                logger.LogError("Remove Migration command failed: {Error}", result.ErrorMessage);
                return CommandResults.Failure(result.ErrorMessage);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Remove Migration command was cancelled.");
            return CommandResults.Canceled();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Remove Migration command failed with exception.");
            return CommandResults.Failure(ex);
        }
    }

    private static async Task<ExecuteCommandResult> ExecuteGetStatusCommandAsync(
        ExecuteCommandContext context,
        EFMigrationResource migrationResource)
    {
        var resourceLoggerService = context.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var interactionService = context.ServiceProvider.GetService<IInteractionService>();
        var logger = resourceLoggerService.GetLogger(migrationResource);
        var contextTypeName = migrationResource.ContextTypeName;

        try
        {
            logger.LogInformation("Getting database status for context {ContextType}...",
                contextTypeName ?? "(auto-detect)");

            var executionContext = context.ServiceProvider.GetRequiredService<DistributedApplicationExecutionContext>();
            await ProcessEnvironmentVariablesAsync(migrationResource, executionContext, context.CancellationToken).ConfigureAwait(false);

            using var executor = new EFCoreOperationExecutor(
                migrationResource.ProjectResource,
                migrationResource.MigrationsProject,
                contextTypeName,
                logger,
                context.CancellationToken);

            var result = await executor.GetDatabaseStatusAsync().ConfigureAwait(false);

            if (result.Success)
            {
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

    /// <summary>
    /// Processes environment variables for the project resource by invoking EnvironmentCallbackAnnotations.
    /// This sets connection string environment variables using the same pattern as when Aspire starts a project.
    /// </summary>
    internal static async Task ProcessEnvironmentVariablesAsync(
        EFMigrationResource migrationResource,
        DistributedApplicationExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        var environmentVariables = new Dictionary<string, object>();

        if (migrationResource.ProjectResource.TryGetAnnotationsOfType<EnvironmentCallbackAnnotation>(out var environmentCallbacks))
        {
            var context = new EnvironmentCallbackContext(
                executionContext,
                migrationResource.ProjectResource,
                environmentVariables,
                cancellationToken: cancellationToken);

            foreach (var callback in environmentCallbacks)
            {
                await callback.Callback(context).ConfigureAwait(false);
            }
        }

        // Set connection strings as environment variables
        const string connectionStringPrefix = "ConnectionStrings__";
        foreach (var (key, value) in environmentVariables)
        {
            if (!key.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resolved = value switch
            {
                string s => s,
                IValueProvider valueProvider => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
                _ => value?.ToString()
            };

            if (!string.IsNullOrEmpty(resolved))
            {
                Environment.SetEnvironmentVariable(key, resolved);
            }
        }
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only
}
