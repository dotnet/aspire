// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Options for <see cref="ServiceEndPointResolver"/>.
/// </summary>
public sealed class ServiceEndPointResolverOptions
{
    /// <summary>
    /// Gets or sets the period between polling resolvers which are in a pending state and do not support refresh notifications via <see cref="IChangeToken.ActiveChangeCallbacks"/>.
    /// </summary>
    public TimeSpan PendingStatusRefreshPeriod { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the period between polling attempts for resolvers which do not support refresh notifications via <see cref="IChangeToken.ActiveChangeCallbacks"/>.
    /// </summary>
    public TimeSpan RefreshPeriod { get; set; } = TimeSpan.FromSeconds(60);
}
