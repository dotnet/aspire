// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using static Aspire.ArgumentExceptionExtensions;

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
    private string _databaseName = ThrowIfNullOrEmpty(databaseName);

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName
    {
        get => _databaseName;
        set => _databaseName = ThrowIfNullOrEmpty(value, nameof(databaseName));
    }

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
    public ReferenceExpression ConnectionStringExpression =>
        Parent.IsEmulator || Parent.UseAccessKeyAuthentication
            ? ReferenceExpression.Create($"{Parent.ConnectionStringExpression};Database={DatabaseName}")
            : ReferenceExpression.Create($"AccountEndpoint={Parent.ConnectionStringExpression};Database={DatabaseName}");

    // ensure Azure Functions projects can WithReference a CosmosDB database
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (Parent.IsEmulator || Parent.UseAccessKeyAuthentication)
        {
            Parent.SetConnectionString(target, connectionName, ConnectionStringExpression);
        }
        else
        {
            Parent.SetAccountEndpoint(target, connectionName);
            target[$"Aspire__Microsoft__EntityFrameworkCore__Cosmos__{connectionName}__DatabaseName"] = DatabaseName;
            target[$"Aspire__Microsoft__Azure__Cosmos__{connectionName}__DatabaseName"] = DatabaseName;
        }
    }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
