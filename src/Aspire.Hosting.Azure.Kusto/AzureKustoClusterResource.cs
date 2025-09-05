// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;

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
    public bool IsEmulator => this.IsEmulator();

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
            // Check if this resource has an HTTP endpoint (which indicates it's running as an emulator)
            // We use try/catch because GetEndpoint throws if the endpoint doesn't exist
            try
            {
                var endpoint = this.GetEndpoint("http");
                // If we got here, we have an HTTP endpoint, so use the emulator connection format
                return ReferenceExpression.Create($"{endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.Host)}:{endpoint.Property(EndpointProperty.Port)}");
            }
            catch (InvalidOperationException)
            {
                // No HTTP endpoint found, so this is an Azure provisioned resource
                return ReferenceExpression.Create($"{ConnectionStringOutput}");
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
}
