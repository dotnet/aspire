// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A network identifier used to specify the network context for resources within an Aspire application model.
/// </summary>
public readonly record struct NetworkIdentifier(string Value);

/// <summary>
/// Provides known network identifiers for use within the Aspire application model API.
/// </summary> 
public static class KnownNetworkIdentifiers
{
    /// <summary>
    /// The network associated with the IP loopback interface (localhost).
    /// </summary>
    public static readonly NetworkIdentifier Localhost = new NetworkIdentifier("localhost");

    /// <summary>
    /// Represents public Internet (globally routable).
    /// </summary>
    public static readonly NetworkIdentifier PublicInternet = new NetworkIdentifier(".");

    /// <summary>
    /// Represents Aspire default, auto-created container network resource (not actual Docker/Podman network).
    /// </summary>
    public static readonly NetworkIdentifier DefaultAspireContainerNetwork = new NetworkIdentifier("aspire-container-network");
}
