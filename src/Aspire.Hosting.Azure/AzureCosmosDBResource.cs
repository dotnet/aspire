// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Cosmos;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="connectionString">The connection string to use to connect.</param>
public class AzureCosmosDBResource(string name, string? connectionString)
    : Resource(name), IResourceWithConnectionString, IAzureResource
{
    private readonly Collection<AzureCosmosDBDatabaseResource> _databases = new();

    /// <summary>
    /// Gets or sets the connection string for the Azure Cosmos DB resource.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString() => IsEmulator
        ? AzureCosmosDBEmulatorConnectionString.Create(GetEmulatorPort("emulator"))
        : ConnectionString;

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets a collection of Azure Cosmos DB database resources.
    /// </summary>
    public IReadOnlyCollection<AzureCosmosDBDatabaseResource> Databases => _databases;

    private int GetEmulatorPort(string endpointName) =>
        Annotations
            .OfType<AllocatedEndpointAnnotation>()
            .FirstOrDefault(x => x.Name == endpointName)
            ?.Port
        ?? throw new DistributedApplicationException($"Azure Cosmos DB resource does not have endpoint annotation with name '{endpointName}'.");

    internal void AddDatabase(AzureCosmosDBDatabaseResource database)
    {
        if (database.Parent != this)
        {
            throw new ArgumentException("Database belongs to another server", nameof(database));
        }
        _databases.Add(database);
    }
}

file static class AzureCosmosDBEmulatorConnectionString
{
    public static string Create(int port) => $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://127.0.0.1:{port};";
}
