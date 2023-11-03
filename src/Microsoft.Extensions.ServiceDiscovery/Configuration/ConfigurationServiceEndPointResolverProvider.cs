// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// <see cref="IServiceEndPointResolverProvider"/> implementation that resolves services using <see cref="IConfiguration"/>.
/// </summary>
/// <param name="configuration">The configuration.</param>
/// <param name="options">The options.</param>
/// <param name="loggerFactory">The logger factory.</param>
public class ConfigurationServiceEndPointResolverProvider(
    IConfiguration configuration,
    IOptions<ConfigurationServiceEndPointResolverOptions> options,
    ILoggerFactory loggerFactory) : IServiceEndPointResolverProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IOptions<ConfigurationServiceEndPointResolverOptions> _options = options;
    private readonly ILogger<ConfigurationServiceEndPointResolver> _logger = loggerFactory.CreateLogger<ConfigurationServiceEndPointResolver>();

    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        resolver = new ConfigurationServiceEndPointResolver(serviceName, _configuration, _logger, _options);
        return true;
    }
}
