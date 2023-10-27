// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ServiceDiscovery.Abstractions;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

/// <summary>
/// Options for configuring <see cref="DnsSrvServiceEndPointResolver"/>.
/// </summary>
public class DnsSrvServiceEndPointResolverOptions
{
    /// <summary>
    /// Gets or sets the default refresh period for endpoints resolved from DNS.
    /// </summary>
    public TimeSpan DefaultRefreshPeriod { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the initial period between retries.
    /// </summary>
    public TimeSpan MinRetryPeriod { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the maximum period between retries.
    /// </summary>
    public TimeSpan MaxRetryPeriod { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the retry period growth factor.
    /// </summary>
    public double RetryBackOffFactor { get; set; } = 2;

    /// <summary>
    /// Gets or sets the default DNS query suffix for services resolved via this provider.
    /// </summary>
    /// <remarks>
    /// If not specified, the provider will attempt to infer the namespace.
    /// </remarks>
    public string? QuerySuffix { get; set; }

    /// <summary>
    /// Gets or sets a delegate used to determine whether to apply host name metadata to each resolved endpoint. Defaults to <c>false</c>.
    /// </summary>
    public Func<ServiceEndPoint, bool> ApplyHostNameMetadata { get; set; } = _ => false;
}
