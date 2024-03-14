// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure SignalR resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureSignalRResource(string name) :
    AzureBicepResource(name, templateResourceName: "Aspire.Hosting.Azure.Bicep.signalr.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for Azure SignalR.
    /// </summary>
    public BicepOutputReference HostName => new("hostName", this);

    private ReferenceExpression ConnectionString
        => ReferenceExpression.Create($"Endpoint=https://{HostName};AuthType=azure");

    /// <summary>
    /// Gets the connection string template for the manifest for Azure SignalR.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string for Azure SignalR.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string for Azure SignalR.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ConnectionString.GetValueAsync(cancellationToken);
    }
}

/// <summary>
/// Represents an Azure SignalR resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="configureConstruct"></param>
public class AzureSignalRConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" output reference from the bicep template for Azure SignalR.
    /// </summary>
    public BicepOutputReference HostName => new("hostName", this);

    private ReferenceExpression ConnectionString
        => ReferenceExpression.Create($"Endpoint=https://{HostName};AuthType=azure");

    /// <summary>
    /// Gets the connection string template for the manifest for Azure SignalR.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string for Azure SignalR.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string for Azure SignalR.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        return ConnectionString.GetValueAsync(cancellationToken);
    }
}
