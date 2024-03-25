// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

/// <summary>
/// <see cref="IServiceEndPointProviderFactory"/> implementation that resolves services using <see cref="IConfiguration"/>.
/// </summary>
internal sealed class ConfigurationServiceEndPointResolverProvider(
    IConfiguration configuration,
    IOptions<ConfigurationServiceEndPointResolverOptions> options,
    IOptions<ServiceDiscoveryOptions> serviceDiscoveryOptions,
    ILogger<ConfigurationServiceEndPointResolver> logger) : IServiceEndPointProviderFactory
{
    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndPointQuery query, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        resolver = new ConfigurationServiceEndPointResolver(query, configuration, logger, options, serviceDiscoveryOptions);
        return true;
    }
}
