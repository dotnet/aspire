// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure AI Search resource.
/// </summary>
/// <param name="name">The name of the resource</param>
/// <param name="configureInfrastructure">Callback to configure the Azure AI Search resource.</param>
public class AzureSearchResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the Azure AI Search resource.
    /// </summary>
    /// <remarks>
    /// This connection string will assume you're deploying to public Azure.
    /// </remarks>
    public BicepOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{ConnectionString}");
}
