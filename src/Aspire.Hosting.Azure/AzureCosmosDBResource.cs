// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// Represents a connection to an Azure Cosmos DB account.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="connectionString">The connection string to use to connect.</param>
public class AzureCosmosDBResource(string name, string? connectionString)
    : Resource(name), IResourceWithConnectionString, IAzureResource
{
    /// <summary>
    /// Gets the well-known and documented connection string for the Azure Cosmos DB emulator.
    /// See <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    internal const string EmulatorConnectionString =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    /// <summary>
    /// Gets or sets the connection string for the Azure Cosmos DB resource.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString() => ConnectionString;
}
