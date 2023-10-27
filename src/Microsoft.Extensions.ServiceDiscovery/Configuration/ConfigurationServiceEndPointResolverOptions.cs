// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Options for <see cref="ConfigurationServiceEndPointResolver"/>.
/// </summary>
public class ConfigurationServiceEndPointResolverOptions
{
    /// <summary>
    /// The name of the configuration section which contains service endpoints. Defaults to <c>"Services"</c>.
    /// </summary>
    public string? SectionName { get; set; } = "Services";

    /// <summary>
    /// Gets or sets a delegate used to determine whether to apply host name metadata to each resolved endpoint. Defaults to <c>false</c>.
    /// </summary>
    public Func<ServiceEndPoint, bool> ApplyHostNameMetadata { get; set; } = _ => false;
}
