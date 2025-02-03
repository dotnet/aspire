// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure SignalR resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
public class AzureSignalRResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure) :
    AzureProvisioningResource(name, configureInfrastructure),
    IResourceWithConnectionString,
    IResourceWithEndpoints
{
    internal EndpointReference EmulatorEndpoint => new(this, "emulator");
    /// <summary>
    /// Gets a value indicating whether the Azure SignalR resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for Azure SignalR.
    /// </summary>
    public BicepOutputReference HostName => new("hostName", this);

    /// <summary>
    /// Gets the connection string template for the manifest for Azure SignalR.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        IsEmulator
        ? ReferenceExpression.Create($"Endpoint={EmulatorEndpoint.Property(EndpointProperty.Url)};AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGH;Version=1.0;")
        : ReferenceExpression.Create($"Endpoint=https://{HostName};AuthType=azure");
}
