// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource builder for EF Core migration resources that wraps an underlying builder
/// and provides additional context type information.
/// </summary>
internal sealed class EFMigrationResourceBuilder : IEFMigrationResourceBuilder
{
    private readonly IResourceBuilder<EFMigrationResource> _innerBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="EFMigrationResourceBuilder"/> class.
    /// </summary>
    /// <param name="innerBuilder">The underlying resource builder.</param>
    /// <param name="contextTypeName">The fully qualified name of the DbContext type, or null to auto-detect.</param>
    public EFMigrationResourceBuilder(IResourceBuilder<EFMigrationResource> innerBuilder, string? contextTypeName)
    {
        _innerBuilder = innerBuilder;
        ContextTypeName = contextTypeName;
    }

    /// <inheritdoc />
    public EFMigrationResource Resource => _innerBuilder.Resource;

    /// <inheritdoc />
    public IDistributedApplicationBuilder ApplicationBuilder => _innerBuilder.ApplicationBuilder;

    /// <inheritdoc />
    public string? ContextTypeName { get; }

    /// <inheritdoc />
    public IResourceBuilder<EFMigrationResource> WithAnnotation<TAnnotation>(TAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append)
        where TAnnotation : IResourceAnnotation
    {
        _innerBuilder.WithAnnotation(annotation, behavior);
        return this;
    }
}
