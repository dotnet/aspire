// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Provisioning.WebPubSub;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Web PubSub Hub setting resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="webpubsub">The <see cref="AzureWebPubSubResource"/> that the resource belongs to.</param>
public class AzureWebPubSubHubResource(string name, AzureWebPubSubResource webpubsub) : Resource(name),
    IResourceWithParent<AzureWebPubSubResource>
{
    private readonly AzureWebPubSubResource _webpubsub = webpubsub ?? throw new ArgumentNullException(nameof(webpubsub));
    /// <summary>
    /// Gets the parent AzureWebPubSubResource of this AzureWebPubSubHubSettingResource.
    /// </summary>
    public AzureWebPubSubResource Parent => _webpubsub;

    internal List<(ReferenceExpression url, string userEvents, string[]? systemEvents, UpstreamAuthSettings? auth)> EventHandlers { get; } = new();
}
