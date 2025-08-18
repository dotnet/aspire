// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// A resource that represents a Kusto cluster.
/// </summary>
public class AzureKustoClusterResource : Resource, IResourceWithConnectionString, IResourceWithEndpoints
{
    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKustoClusterResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public AzureKustoClusterResource(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Gets whether the resource is running the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsEmulator();

    /// <inheritdoc/>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            var endpoint = this.GetEndpoint("http");
            return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");
        }
    }

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    internal IReadOnlyDictionary<string, string> Databases => _databases;

    internal void AddDatabase(string name, string databaseName)
    {
        _databases.TryAdd(name, databaseName);
    }
}
