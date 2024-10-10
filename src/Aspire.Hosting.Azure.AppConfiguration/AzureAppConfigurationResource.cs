// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents Azure App Configuration.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct">Callback to populate the construct with Azure resources.</param>
public class AzureAppConfigurationResource(string name, Action<AzureResourceInfrastructure> configureConstruct) :
    AzureProvisioningResource(name, configureConstruct),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the appConfigEndpoint output reference for the Azure App Configuration resource.
    /// </summary>
    public BicepOutputReference Endpoint => new("appConfigEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure App Configuration resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create($"{Endpoint}");
}
