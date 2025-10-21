// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides known network identifiers for use within the Aspire application model API.
/// </summary> 
public static class KnownNetworkIdentifiers
{
    /// <summary>
    /// The network associated with the IP loopback interface (localhost).
    /// </summary>
    public const string Localhost = "localhost";

    /// <summary>
    /// Represents public Internet (globally routable).
    /// </summary>
    public const string PublicInternet = ".";

    /// <summary>
    /// Represents Aspire default, auto-created container network resource (not actual Docker/Podman network).
    /// </summary>
    public const string DefaultAspireContainerNetwork = "aspire-container-network";
}
