// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has a connection string associated with it.
/// </summary>
public interface IResourceWithConnectionString : IResource, IManifestExpressionProvider, IValueProvider
{
    /// <summary>
    /// Gets the connection string associated with the resource.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string associated with the resource, when one is available.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default) =>
        ConnectionStringExpression.GetValueAsync(cancellationToken);

    string IManifestExpressionProvider.ValueExpression => ConnectionStringReferenceExpression;

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => GetConnectionStringAsync(cancellationToken);

    /// <summary>
    /// Describes the connection string format string used for this resource in the manifest.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression { get; }

    /// <summary>
    /// The expression used in the manifest to reference the connection string.
    /// </summary>
    public string ConnectionStringReferenceExpression => $"{{{Name}.connectionString}}";

    /// <summary>
    /// The environment variable name to use for the connection string.
    /// </summary>
    public string? ConnectionStringEnvironmentVariable => null;
}
