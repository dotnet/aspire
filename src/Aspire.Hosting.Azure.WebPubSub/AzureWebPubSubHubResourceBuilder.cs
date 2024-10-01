// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// The resource builder for configuring Web PubSub hub settings
/// </summary>
public class AzureWebPubSubHubResourceBuilder
{
    internal AzureWebPubSubHubResourceBuilder(IResourceBuilder<AzureWebPubSubResource> builder, string hubName)
    {
        Builder = builder;
        HubName = hubName;
    }

    /// <summary>
    /// The parent Web PubSub resource builder
    /// </summary>
    public IResourceBuilder<AzureWebPubSubResource> Builder { get; }

    internal string HubName { get; }
}
