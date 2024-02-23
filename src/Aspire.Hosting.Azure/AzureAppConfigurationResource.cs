// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents Azure App Configuration.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureAppConfigurationResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.appconfig.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the appConfigEndpoint output reference for the Azure App Configuration resource.
    /// </summary>
    public BicepOutputReference Endpoint => new("appConfigEndpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure App Configuration resource.
    /// </summary>
    public string ConnectionStringExpression => Endpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for the Azure App Configuration resource.
    /// </summary>
    /// <returns>The connection string for the Azure App Configuration resource.</returns>
    public string? GetConnectionString() => Endpoint.Value;
}
