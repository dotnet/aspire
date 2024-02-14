// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB database. This is a child resource of a <see cref="MongoDBServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The MongoDB server resource associated with this database.</param>
public class MongoDBDatabaseResource(string name, MongoDBServerResource parent) : Resource(name), IResourceWithParent<MongoDBServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent MongoDB container resource.
    /// </summary>
    public MongoDBServerResource Parent => parent;

    /// <summary>
    /// Gets the connection string for the MongoDB database.
    /// </summary>
    /// <returns>A connection string for the MongoDB database.</returns>
    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return connectionString.EndsWith('/') ?
                $"{connectionString}{Name}" :
                $"{connectionString}/{Name}";
        }

        throw new DistributedApplicationException("Parent resource connection string was null.");
    }
}
