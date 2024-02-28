// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Web PubSub resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureWebPubSubResource(string name) :
    AzureBicepResource(name, templateResourceName: "Aspire.Hosting.Azure.Bicep.webpubsub.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "endpoint" output reference from the bicep template for Azure Web PubSub.
    /// </summary>
    public BicepOutputReference Endpoint => new("endpoint", this);

    /// <summary>
    /// Gets the connection string template for the manifest for Azure Web PubSub.
    /// </summary>
    public string ConnectionStringExpression => Endpoint.ValueExpression;

    /// <summary>
    /// Gets the connection string for Azure Web PubSub which is actually the service Endpoint URL.
    /// </summary>
    /// <returns>The connection string for Azure Web PubSub.</returns>
    public string? GetConnectionString() => Endpoint.Value;

    /// <summary>
    /// Gets the connection string for Azure Web PubSub.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string for Azure Web PubSub.</returns>
    public async ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (ProvisioningTaskCompletionSource is not null)
        {
            await ProvisioningTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        return await Endpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);
    }
}
