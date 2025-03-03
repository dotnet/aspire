// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Log Analytics Workspace resource.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure Log Analytics Workspace resource.</param>
public class AzureLogAnalyticsWorkspaceResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
    : AzureProvisioningResource(name, configureInfrastructure)
{
    /// <summary>
    /// Gets the "logAnalyticsWorkspaceId" output reference for the Azure Log Analytics Workspace resource.
    /// </summary>
    public BicepOutputReference WorkspaceId => new("logAnalyticsWorkspaceId", this);
}
