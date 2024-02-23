// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure SignalR resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureSignalRResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.signalr.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for Azure SignalR.
    /// </summary>
    public BicepOutputReference HostName => new("hostName", this);

    /// <summary>
    /// Gets the connection string template for the manifest for Azure SignalR.
    /// </summary>
    public string ConnectionStringExpression => $"Endpoint=https://{HostName.ValueExpression};AuthType=azure";
    /// <summary>
    /// Gets the connection string for Azure SignalR.
    /// </summary>
    /// <returns>The connection string for Azure SignalR.</returns>
    public string? GetConnectionString() => $"Endpoint=https://{HostName.Value};AuthType=azure";
}
