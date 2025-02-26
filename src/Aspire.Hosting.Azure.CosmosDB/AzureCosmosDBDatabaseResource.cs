// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cosmos DB Database.
/// Initializes a new instance of the <see cref="AzureCosmosDBDatabaseResource"/> class.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureCosmosDBDatabaseResource(string name, string databaseName, AzureCosmosDBResource parent)
    : Resource(name), IResourceWithParent<AzureCosmosDBResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; } = ThrowIfNullOrEmpty(databaseName);

    /// <summary>
    /// The containers for this database.
    /// </summary>
    internal List<AzureCosmosDBContainerResource> Containers { get; } = [];

    /// <summary>
    /// Gets the parent Azure Cosmos DB account resource.
    /// </summary>
    public AzureCosmosDBResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    // ensure Azure Functions projects can WithReference a CosmosDB database
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName) =>
        ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
