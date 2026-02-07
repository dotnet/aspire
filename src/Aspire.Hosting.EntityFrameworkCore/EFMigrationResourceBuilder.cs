// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// A resource builder for EF Core migration resources that wraps an underlying builder
/// and provides additional context type information.
/// </summary>
public sealed class EFMigrationResourceBuilder : IResourceBuilder<EFMigrationResource>
{
    private readonly IResourceBuilder<EFMigrationResource> _innerBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFMigrationResourceBuilder"/> class.
    /// </summary>
    /// <param name="innerBuilder">The underlying resource builder.</param>
    internal EFMigrationResourceBuilder(IResourceBuilder<EFMigrationResource> innerBuilder)
    {
        _innerBuilder = innerBuilder;
    }

    /// <inheritdoc />
    public EFMigrationResource Resource => _innerBuilder.Resource;

    /// <inheritdoc />
    public IDistributedApplicationBuilder ApplicationBuilder => _innerBuilder.ApplicationBuilder;

    /// <inheritdoc />
    public IResourceBuilder<EFMigrationResource> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append)
        where TAnnotation : IResourceAnnotation
    {
        _innerBuilder.WithAnnotation(annotation, behavior);
        return this;
    }

    /// <summary>
    /// Configures the EF migration resource to run database update when the AppHost starts.
    /// </summary>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// When enabled, migrations will be applied during AppHost startup. The resource state will transition
    /// from "Pending" to "Running" to "Finished" (or "FailedToStart" on error).
    /// </para>
    /// <para>
    /// A health check is automatically registered for this resource, allowing other resources to use
    /// <c>.WaitFor()</c> to wait until migrations complete before starting.
    /// </para>
    /// </remarks>
    public EFMigrationResourceBuilder RunDatabaseUpdateOnStart()
    {
        var migrationResource = Resource;
        ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
        {
            // Schedule the migration command to run asynchronously after startup completes to avoid deadlocks.
            var _ = ExecuteMigrationsAsync(@event.Services, migrationResource, ct);
            return Task.CompletedTask;
        });

        return this;
    }

    private static async Task ExecuteMigrationsAsync(
        IServiceProvider serviceProvider,
        EFMigrationResource migrationResource,
        CancellationToken cancellationToken)
    {
        var resourceCommandService = serviceProvider.GetRequiredService<ResourceCommandService>();

        try
        {
            var result = await resourceCommandService.ExecuteCommandAsync(
                migrationResource,
                "ef-database-update",
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Application is shutting down
        }
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration script during publishing.
    /// </summary>
    /// <param name="idempotent">If <c>true</c>, generates an idempotent script with IF NOT EXISTS checks.</param>
    /// <param name="noTransactions">If <c>true</c>, omits transaction statements from the script.</param>
    /// <returns>The resource builder for chaining.</returns>
    public EFMigrationResourceBuilder PublishAsMigrationScript(bool idempotent = false, bool noTransactions = false)
    {
        Resource.PublishAsMigrationScript = true;
        Resource.ScriptIdempotent = idempotent;
        Resource.ScriptNoTransactions = noTransactions;
        return this;
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration bundle during publishing.
    /// </summary>
    /// <param name="targetRuntime">The target runtime identifier for the bundle (e.g., "linux-x64", "win-x64"). If null, uses the current runtime.</param>
    /// <param name="selfContained">If <c>true</c>, creates a self-contained bundle that includes the .NET runtime.</param>
    /// <returns>The resource builder for chaining.</returns>
    public EFMigrationResourceBuilder PublishAsMigrationBundle(string? targetRuntime = null, bool selfContained = false)
    {
        Resource.PublishAsMigrationBundle = true;
        Resource.BundleTargetRuntime = targetRuntime;
        Resource.BundleSelfContained = selfContained;
        return this;
    }

    /// <summary>
    /// Configures the output directory for new migrations created with the Add Migration command.
    /// </summary>
    /// <param name="outputDirectory">The output directory path relative to the project root.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// If not specified, migrations will be placed in the default 'Migrations' directory.
    /// Example: "Data/Migrations" or "Infrastructure/Migrations".
    /// </remarks>
    public EFMigrationResourceBuilder WithMigrationOutputDirectory(string outputDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);
        Resource.MigrationOutputDirectory = outputDirectory;
        return this;
    }

    /// <summary>
    /// Configures the namespace for new migrations created with the Add Migration command.
    /// </summary>
    /// <param name="namespace">The namespace for generated migrations.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// If not specified, the namespace will be derived from the project's default namespace.
    /// Example: "MyApp.Data.Migrations" or "MyApp.Infrastructure.Migrations".
    /// </remarks>
    public EFMigrationResourceBuilder WithMigrationNamespace(string @namespace)
    {
        ArgumentException.ThrowIfNullOrEmpty(@namespace);
        Resource.MigrationNamespace = @namespace;
        return this;
    }

    /// <summary>
    /// Configures a separate project containing the migrations using a project path.
    /// </summary>
    /// <param name="projectPath">The path to the project file containing the migrations.</param>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when the migrations are in a different project than the startup project.
    /// The target project's path will be used for migration operations while the startup project
    /// remains the original project.
    /// </para>
    /// </remarks>
    public EFMigrationResourceBuilder WithMigrationsProject(string projectPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectPath);
        
        projectPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(_innerBuilder.ApplicationBuilder.AppHostDirectory, projectPath));

        Resource.MigrationsProjectMetadata = new ProjectMetadata(projectPath);
        return this;
    }

    /// <summary>
    /// Configures a separate project containing the migrations using a project metadata type.
    /// </summary>
    /// <typeparam name="TProject">The project metadata type generated by the Aspire build tooling.</typeparam>
    /// <returns>The resource builder for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Use this method when the migrations are in a different project than the startup project.
    /// The target project's path will be used for migration operations while the startup project
    /// remains the original project.
    /// </para>
    /// <example>
    /// <code>
    /// var migrations = project.AddEFMigrations&lt;MyDbContext&gt;("migrations")
    ///     .WithMigrationsProject&lt;Projects.MyMigrationsProject&gt;();
    /// </code>
    /// </example>
    /// </remarks>
    public EFMigrationResourceBuilder WithMigrationsProject<TProject>()
        where TProject : IProjectMetadata, new()
    {
        Resource.MigrationsProjectMetadata = new TProject();
        return this;
    }

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override string? ToString() => base.ToString();

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override bool Equals(object? obj) => base.Equals(obj);

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public override int GetHashCode() => base.GetHashCode();
}
