// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has a connection string associated with it.
/// </summary>
public interface IResourceWithConnectionString : IResource, IManifestExpressionProvider, IValueProvider, IValueWithReferences
{
    /// <summary>
    /// Gets the connection string associated with the resource.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string associated with the resource, when one is available.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default) =>
        ConnectionStringExpression.GetValueAsync(cancellationToken);

    string IManifestExpressionProvider.ValueExpression => $"{{{Name}.connectionString}}";

    ValueTask<string?> IValueProvider.GetValueAsync(CancellationToken cancellationToken) => GetConnectionStringAsync(cancellationToken);

    /// <summary>
    /// Describes the connection string format string used for this resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression { get; }

    /// <summary>
    /// The environment variable name to use for the connection string.
    /// </summary>
    public string? ConnectionStringEnvironmentVariable => null;

    IEnumerable<object> IValueWithReferences.References => [ConnectionStringExpression];

    /// <summary>
    /// Retrieves a read-only collection of key-value pairs that describe the current connection's properties.
    /// </summary>
    /// <remarks>The returned dictionary may include provider-specific properties such as server version,
    /// connection state, or authentication details. The set of available properties depends on the implementation and
    /// the current connection context.</remarks>
    /// <returns>An <see cref="IReadOnlyDictionary{String, Object}"/> containing the names and values of the connection
    /// properties. The dictionary is empty if no properties are available.</returns>
    IReadOnlyDictionary<string, ReferenceExpression> GetConnectionProperties() => new Dictionary<string, ReferenceExpression>();
}

/// <summary>
/// Represents a resource that exposes a set of properties of type <typeparamref name="T"/> and provides access via a
/// connection string.
/// </summary>
/// <typeparam name="T">The type of the properties associated with the resource.</typeparam>
public interface IResourceWithConnectionProperties<T> : IResourceWithConnectionString where T : struct
{
}
