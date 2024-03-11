// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Options for service endpoint resolvers.
/// </summary>
public sealed class ServiceDiscoveryOptions
{
    /// <summary>
    /// The value for <see cref="AllowedSchemes"/> which indicates that all schemes are allowed.
    /// </summary>
#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable CA1825 // Avoid zero-length array allocations
    public static readonly string[] AllSchemes = new string[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations
#pragma warning restore IDE0300 // Simplify collection initialization

    /// <summary>
    /// Gets or sets the period between polling resolvers which are in a pending state and do not support refresh notifications via <see cref="IChangeToken.ActiveChangeCallbacks"/>.
    /// </summary>
    public TimeSpan PendingStatusRefreshPeriod { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the period between polling attempts for resolvers which do not support refresh notifications via <see cref="IChangeToken.ActiveChangeCallbacks"/>.
    /// </summary>
    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the collection of allowed URI schemes for URIs resolved by the service discovery system when multiple schemes are specified, for example "https+http://_endpoint.service".
    /// </summary>
    /// <remarks>
    /// When set to <see cref="AllSchemes"/>, all schemes are allowed.
    /// Schemes are not case-sensitive.
    /// </remarks>
    public string[] AllowedSchemes { get; set; } = AllSchemes;
}
