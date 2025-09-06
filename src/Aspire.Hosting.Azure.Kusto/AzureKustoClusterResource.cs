// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Kusto;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// A resource that represents a Kusto cluster.
/// </summary>
public class AzureKustoClusterResource : AzureProvisioningResource, IResourceWithConnectionString, IResourceWithEndpoints
{
    private readonly Dictionary<string, string> _databases = new(StringComparers.ResourceName);

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKustoClusterResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    public AzureKustoClusterResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
    }

    /// <summary>
    /// Gets whether the resource is running the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the cluster URI output reference for the Azure Kusto cluster.
    /// </summary>
    public BicepOutputReference ClusterUri => new("clusterUri", this);

    /// <summary>
    /// Gets the connection string output reference for the Azure Kusto cluster.
    /// </summary>
    /// <remarks>
    /// TODO: Implement proper connection string output for Azure provisioned Kusto clusters.
    /// This is a placeholder that will need to be refined when full Azure provisioning support is added.
    /// </remarks>
    public BicepOutputReference ConnectionStringOutput => new("connectionString", this);

    /// <inheritdoc/>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            // Check if this resource is marked as an emulator
            if (IsEmulator)
            {
                // For emulator, use the HTTP endpoint pattern
                var endpoint = this.GetEndpoint("http");
                return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");
            }
            else
            {
                // For Azure provisioned resources, use the cluster URI output
                return ReferenceExpression.Create($"{ClusterUri}");
            }
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

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();

        // Check if a KustoCluster with the same identifier already exists
        var existingCluster = resources.OfType<KustoCluster>().SingleOrDefault(cluster => cluster.BicepIdentifier == bicepIdentifier);

        if (existingCluster is not null)
        {
            return existingCluster;
        }

        // Create and add new resource if it doesn't exist
        var cluster = KustoCluster.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceNameAndScope(
            this,
            infra,
            cluster))
        {
            cluster.Name = NameOutputReference.AsProvisioningParameter(infra);
        }

        infra.Add(cluster);
        return cluster;
    }
}
