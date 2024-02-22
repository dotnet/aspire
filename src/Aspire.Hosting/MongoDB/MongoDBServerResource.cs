// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.MongoDB;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MongoDB container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class MongoDBServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    public string ConnectionStringExpression =>
        $"mongodb://{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}";

    /// <summary>
    /// Gets the connection string for the MongoDB server.
    /// </summary>
    /// <returns>A connection string for the MongoDB server in the form "mongodb://host:port".</returns>
    public string? GetConnectionString()
    {
        if (!this.TryGetAllocatedEndPoints(out var allocatedEndpoints))
        {
            throw new DistributedApplicationException("Expected allocated endpoints!");
        }

        var allocatedEndpoint = allocatedEndpoints.Single();

        return new MongoDBConnectionStringBuilder()
            .WithServer(allocatedEndpoint.Address)
            .WithPort(allocatedEndpoint.Port)
            .Build();
    }

    private readonly Dictionary<string, string> _databases = new Dictionary<string, string>(StringComparers.ResourceName);

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
