// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a SurrealDB namespace that is a child of a SurrealDB container resource.
/// </summary>
public class SurrealDbNamespaceResource : Resource, IResourceWithParent<SurrealDbServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent SurrealDB container resource.
    /// </summary>
    public SurrealDbServerResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the SurrealDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{Parent};Namespace={NamespaceName}");

    /// <summary>
    /// Gets the namespace name.
    /// </summary>
    public string NamespaceName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurrealDbNamespaceResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="namespaceName">The namespace name.</param>
    /// <param name="parent">The parent SurrealDB server resource.</param>
    public SurrealDbNamespaceResource(string name, string namespaceName, SurrealDbServerResource parent) : base(name)
    {
        ArgumentException.ThrowIfNullOrEmpty(namespaceName);
        ArgumentNullException.ThrowIfNull(parent);

        NamespaceName = namespaceName;
        Parent = parent;
    }

    private readonly Dictionary<string, string> _databases = new(StringComparer.Ordinal);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
