// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Aspire.Hosting.ApplicationModel;

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
    /// <param name="contextTypeName">The fully qualified name of the DbContext type, or null to auto-detect.</param>
    internal EFMigrationResourceBuilder(IResourceBuilder<EFMigrationResource> innerBuilder, string? contextTypeName)
    {
        _innerBuilder = innerBuilder;
        ContextTypeName = contextTypeName;
    }

    /// <inheritdoc />
    public EFMigrationResource Resource => _innerBuilder.Resource;

    /// <inheritdoc />
    public IDistributedApplicationBuilder ApplicationBuilder => _innerBuilder.ApplicationBuilder;

    /// <summary>
    /// Gets the fully qualified name of the DbContext type to manage migrations for, or <see langword="null"/> to auto-detect.
    /// </summary>
    public string? ContextTypeName { get; }

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
    public EFMigrationResourceBuilder RunDatabaseUpdateOnStart()
    {
        Resource.Options.RunDatabaseUpdateOnStart = true;
        return this;
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration script during publishing.
    /// </summary>
    /// <returns>The resource builder for chaining.</returns>
    public EFMigrationResourceBuilder PublishAsMigrationScript()
    {
        Resource.Options.PublishAsMigrationScript = true;
        return this;
    }

    /// <summary>
    /// Configures the EF migration resource to generate a migration bundle during publishing.
    /// </summary>
    /// <returns>The resource builder for chaining.</returns>
    public EFMigrationResourceBuilder PublishAsMigrationBundle()
    {
        Resource.Options.PublishAsMigrationBundle = true;
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
