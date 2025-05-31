// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB database. This is a child resource of a <see cref="MongoDBServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The MongoDB server resource associated with this database.</param>
public class MongoDBDatabaseResource(string name, string databaseName, MongoDBServerResource parent)
    : Resource(name), IResourceWithParent<MongoDBServerResource>, IResourceWithConnectionString, IResourceWithDirectConnectionString
{
    /// <summary>
    /// Gets the connection string expression for the MongoDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.BuildConnectionString(DatabaseName);

    /// <summary>
    /// Gets the direct connection string expression for the MongoDB database.
    /// </summary>
    /// <remarks>
    /// This is useful to connect to the resource when replica sets are enabled. In those cases, the database will only
    /// accept the registered name for the replica, which is only accessible from within the container network.
    /// </remarks>
    public ReferenceExpression DirectConnectionStringExpression => Parent.BuildConnectionString(DatabaseName, directConnection: true);

    /// <summary>
    /// Gets the parent MongoDB container resource.
    /// </summary>
    public MongoDBServerResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = ThrowIfNullOrEmpty(databaseName);

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
