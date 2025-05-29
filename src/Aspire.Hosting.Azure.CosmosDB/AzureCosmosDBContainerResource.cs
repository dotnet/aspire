// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cosmos DB Database Container.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureCosmosDBContainerResource : Resource, IResourceWithParent<AzureCosmosDBDatabaseResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBContainerResource"/> class.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="containerName">The container name.</param>
    /// <param name="partitionKeyPaths">The hierarchical partition key paths.</param>
    /// <param name="parent">The parent Azure Cosmos DB database resource.</param>
    public AzureCosmosDBContainerResource(string name, string containerName, IEnumerable<string> partitionKeyPaths, AzureCosmosDBDatabaseResource parent) : base(name)
    {
        ContainerName = ThrowIfNullOrEmpty(containerName);
        ArgumentNullException.ThrowIfNull(partitionKeyPaths);
        var partitionKeyPathsArray = partitionKeyPaths.ToArray();
        if (partitionKeyPathsArray.Length == 0)
        {
            throw new ArgumentException("At least one partition key path should be provided.", nameof(partitionKeyPaths));
        }
        if (partitionKeyPaths.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException("Partition key paths cannot contain null or empty strings.", nameof(partitionKeyPaths));
        }

        _partitionKeyPaths = partitionKeyPathsArray;
        Parent = parent ?? throw new ArgumentNullException(nameof(parent));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBContainerResource"/> class.
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="containerName">The container name.</param>
    /// <param name="partitionKeyPath">The partition key path.</param>
    /// <param name="parent">The parent Azure Cosmos DB database resource.</param>
    [Obsolete("Use the overload that supports the hierarchical partition keys instead.")]
    public AzureCosmosDBContainerResource(string name, string containerName, string partitionKeyPath, AzureCosmosDBDatabaseResource parent)
        : this(name, containerName, [partitionKeyPath], parent) { }

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; }

    private IReadOnlyList<string> _partitionKeyPaths;

    /// <summary>
    /// Gets or sets the partition key path.
    /// </summary>
    [Obsolete($"Use {nameof(PartitionKeyPaths)} instead.")]
    public string PartitionKeyPath
    {
        get => _partitionKeyPaths[0];
        set => _partitionKeyPaths = [ThrowIfNullOrEmpty(value)];
    }

    /// <summary>
    /// Gets or sets the hierarchical partition keys.
    /// </summary>
    public IReadOnlyList<string> PartitionKeyPaths
    {
        get => _partitionKeyPaths;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Count == 0)
            {
                throw new ArgumentException("At least one partition key path should be provided.", nameof(value));
            }
            if (value.Any(string.IsNullOrEmpty))
            {
                throw new ArgumentException("Partition key paths cannot contain null or empty strings.", nameof(value));
            }

            _partitionKeyPaths = value;
        }
    }

    /// <summary>
    /// Gets the parent Azure Cosmos DB database resource.
    /// </summary>
    public AzureCosmosDBDatabaseResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB Database Container.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.Parent.GetChildConnectionString(Name, Parent.DatabaseName, ContainerName);

    // ensure Azure Functions projects can WithReference a CosmosDB database container
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (Parent.Parent.IsEmulator || Parent.Parent.UseAccessKeyAuthentication)
        {
            Parent.Parent.SetConnectionString(target, connectionName, ConnectionStringExpression);
        }
        else
        {
            Parent.Parent.SetAccountEndpoint(target, connectionName);
            target[$"Aspire__Microsoft__EntityFrameworkCore__Cosmos__{connectionName}__DatabaseName"] = Parent.DatabaseName;
            target[$"Aspire__Microsoft__Azure__Cosmos__{connectionName}__DatabaseName"] = Parent.DatabaseName;
            target[$"Aspire__Microsoft__EntityFrameworkCore__Cosmos__{connectionName}__ContainerName"] = ContainerName;
            target[$"Aspire__Microsoft__Azure__Cosmos__{connectionName}__ContainerName"] = ContainerName;
        }
    }

    private static string ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
        return argument;
    }
}
