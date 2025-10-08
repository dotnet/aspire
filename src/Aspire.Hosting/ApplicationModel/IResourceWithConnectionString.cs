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
    /// Gets a dictionary containing expressions for connection-related properties, keyed by property name.
    /// </summary>
    /// <remarks>Property names are compared using case-insensitive ordinal comparison. The dictionary
    /// includes an entry for the connection string expression under the key "ConnectionString".</remarks>
    public ReferenceExpression GetProperty(string key) => ReferenceExpression.Create($"");
}

/// <summary>
/// Represents a resource that exposes a set of properties of type <typeparamref name="T"/> and provides access via a
/// connection string.
/// </summary>
/// <typeparam name="T">The type of the properties associated with the resource.</typeparam>
public interface IResourceWithProperties<T> : IResourceWithConnectionString where T : struct
{
    /// <summary>
    /// Retrieves the value of a connection property associated with the specified key.
    /// </summary>
    /// <param name="key">The key used to identify the connection property. The key is converted to its string representation to perform
    /// the lookup.</param>
    /// <returns>The value of the connection property associated with the specified key, or null if the property does not exist
    /// or the property collection is not initialized.</returns>
    public ReferenceExpression GetProperty(T key) => ReferenceExpression.Create($"");

    ReferenceExpression IResourceWithConnectionString.GetProperty(string key) => GetProperty(Enum.Parse<T>(key));
}
