// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning.Primitives;
using Azure.Provisioning.Search;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure AI Search resource.
/// </summary>
/// <param name="name">The name of the resource</param>
/// <param name="configureInfrastructure">Callback to configure the Azure AI Search resource.</param>
public class AzureSearchResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the Azure AI Search resource.
    /// </summary>
    /// <remarks>
    /// This connection string will assume you're deploying to public Azure.
    /// </remarks>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the "name" output reference for the resource.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("name", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var store = SearchService.FromExisting(this.GetBicepIdentifier());
        store.Name = NameOutputReference.AsProvisioningParameter(infra);
        infra.Add(store);
        return store;
    }
}
