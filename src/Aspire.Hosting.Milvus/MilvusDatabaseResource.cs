// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Milvus;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Milvus database. This is a child resource of a <see cref="MilvusServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="parent">The Milvus parent resource associated with this database.</param>
public class MilvusDatabaseResource(string name, string databaseName, MilvusServerResource parent) : Resource(name), IResourceWithParent<MilvusServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent Milvus container resource.
    /// </summary>
    public MilvusServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the Milvus database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Parent};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;
}
