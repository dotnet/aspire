// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Storage;

/// <summary>
/// A resource builder for Azure Storage resources that provides access to child service builders.
/// </summary>
public sealed class AzureStorageResourceBuilder : IResourceBuilder<AzureStorageResource>
{
    private IResourceBuilder<AzureBlobStorageResource>? _blobServiceBuilder;
    private Func<IResourceBuilder<AzureBlobStorageResource>>? _blobServiceFactory;
    private readonly IResourceBuilder<AzureStorageResource> _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureStorageResourceBuilder"/> class.
    /// </summary>
    /// <param name="inner">The inner resource builder.</param>
    public AzureStorageResourceBuilder(IResourceBuilder<AzureStorageResource> inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        _inner = inner;
    }

    /// <summary>
    /// Gets the blob service resource builder for this storage account.
    /// </summary>
    public IResourceBuilder<AzureBlobStorageResource> BlobService => _blobServiceBuilder ??= CreateBlobService();

    /// <inheritdoc />
    public IDistributedApplicationBuilder ApplicationBuilder => _inner.ApplicationBuilder;

    /// <inheritdoc />
    public AzureStorageResource Resource => _inner.Resource;

    /// <inheritdoc />
    public IResourceBuilder<AzureStorageResource> WithAnnotation<T>(T annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append) where T : IResourceAnnotation
    {
        _inner.WithAnnotation(annotation, behavior);
        return this;
    }

    /// <summary>
    /// Sets the factory function for creating the blob service resource builder.
    /// </summary>
    /// <param name="factory">The factory function.</param>
    internal void SetBlobServiceFactory(Func<IResourceBuilder<AzureBlobStorageResource>> factory)
    {
        _blobServiceFactory = factory;
    }

    private IResourceBuilder<AzureBlobStorageResource> CreateBlobService()
    {
        if (_blobServiceFactory is null)
        {
            throw new InvalidOperationException("Blob service factory has not been set. This should be set by the AddAzureStorage extension method.");
        }

        var builder = _blobServiceFactory();
        return builder;
    }
}
