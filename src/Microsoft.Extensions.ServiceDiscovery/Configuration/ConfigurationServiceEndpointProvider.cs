// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

/// <summary>
/// A service endpoint provider that uses configuration to resolve resolved.
/// </summary>
internal sealed partial class ConfigurationServiceEndpointProvider : IServiceEndpointProvider, IHostNameFeature
{
    private const string DefaultEndpointName = "default";
    private readonly string _serviceName;
    private readonly string? _endpointName;
    private readonly bool _includeAllSchemes;
    private readonly string[] _schemes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationServiceEndpointProvider> _logger;
    private readonly IOptions<ConfigurationServiceEndpointProviderOptions> _options;

    /// <summary>
    /// Initializes a new <see cref="ConfigurationServiceEndpointProvider"/> instance.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">Configuration provider options.</param>
    /// <param name="serviceDiscoveryOptions">Service discovery options.</param>
    public ConfigurationServiceEndpointProvider(
        ServiceEndpointQuery query,
        IConfiguration configuration,
        ILogger<ConfigurationServiceEndpointProvider> logger,
        IOptions<ConfigurationServiceEndpointProviderOptions> options,
        IOptions<ServiceDiscoveryOptions> serviceDiscoveryOptions)
    {
        _serviceName = query.ServiceName;
        _endpointName = query.EndpointName;
        _includeAllSchemes = serviceDiscoveryOptions.Value.AllowAllSchemes && query.IncludedSchemes.Count == 0;
        _schemes = ServiceDiscoveryOptions.ApplyAllowedSchemes(query.IncludedSchemes, serviceDiscoveryOptions.Value.AllowedSchemes, serviceDiscoveryOptions.Value.AllowAllSchemes);
        _configuration = configuration;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc/>
    public ValueTask PopulateAsync(IServiceEndpointBuilder endpoints, CancellationToken cancellationToken)
    {
        // Only add resolved to the collection if a previous provider (eg, an override) did not add them.
        if (endpoints.Endpoints.Count != 0)
        {
            Log.SkippedResolution(_logger, _serviceName, "Collection has existing endpoints");
            return default;
        }

        // Get the corresponding config section.
        var section = _configuration.GetSection(_options.Value.SectionName).GetSection(_serviceName);
        if (!section.Exists())
        {
            endpoints.AddChangeToken(_configuration.GetReloadToken());
            Log.ServiceConfigurationNotFound(_logger, _serviceName, $"{_options.Value.SectionName}:{_serviceName}");
            return default;
        }

        endpoints.AddChangeToken(section.GetReloadToken());

        // Find an appropriate configuration section based on the input.
        IConfigurationSection? namedSection = null;
        string endpointName;
        if (string.IsNullOrWhiteSpace(_endpointName))
        {
            // Treat the scheme as the endpoint name and use the first section with a matching endpoint name which exists
            endpointName = DefaultEndpointName;
            ReadOnlySpan<string> candidateNames = [DefaultEndpointName, .. _schemes];
            foreach (var scheme in candidateNames)
            {
                var candidate = section.GetSection(scheme);
                if (candidate.Exists())
                {
                    endpointName = scheme;
                    namedSection = candidate;
                    break;
                }
            }
        }
        else
        {
            // Use the section corresponding to the endpoint name.
            endpointName = _endpointName;
            namedSection = section.GetSection(_endpointName);
        }

        var configPath = $"{_options.Value.SectionName}:{_serviceName}:{endpointName}";
        if (!namedSection.Exists())
        {
            Log.EndpointConfigurationNotFound(_logger, endpointName, _serviceName, configPath);
            return default;
        }

        List<ServiceEndpoint> resolved = [];
        Log.UsingConfigurationPath(_logger, configPath, endpointName, _serviceName);

        // Account for both the single and multi-value cases.
        if (!string.IsNullOrWhiteSpace(namedSection.Value))
        {
            // Single value case.
            AddEndpoint(resolved, namedSection, endpointName);
        }
        else
        {
            // Multiple value case.
            foreach (var child in namedSection.GetChildren())
            {
                if (!int.TryParse(child.Key, out _))
                {
                    throw new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' endpoint '{endpointName}' has non-numeric keys.");
                }

                AddEndpoint(resolved, child, endpointName);
            }
        }

        int resolvedEndpointCount;
        if (_includeAllSchemes)
        {
            // Include all endpoints.
            foreach (var ep in resolved)
            {
                endpoints.Endpoints.Add(ep);
            }
            
            resolvedEndpointCount = resolved.Count;
        }
        else
        {
            // Filter the resolved endpoints to only include those which match the specified, allowed schemes.
            resolvedEndpointCount = 0;
            var minIndex = _schemes.Length;
            foreach (var ep in resolved)
            {
                if (ep.EndPoint is UriEndPoint uri && uri.Uri.Scheme is { } scheme)
                {
                    var index = Array.IndexOf(_schemes, scheme);
                    if (index >= 0 && index < minIndex)
                    {
                        minIndex = index;
                    }
                }
            }

            foreach (var ep in resolved)
            {
                if (ep.EndPoint is UriEndPoint uri && uri.Uri.Scheme is { } scheme)
                {
                    var index = Array.IndexOf(_schemes, scheme);
                    if (index >= 0 && index <= minIndex)
                    {
                        ++resolvedEndpointCount;
                        endpoints.Endpoints.Add(ep);
                    }
                }
                else
                {
                    ++resolvedEndpointCount;
                    endpoints.Endpoints.Add(ep);
                }
            }
        }

        if (resolvedEndpointCount == 0)
        {
            Log.ServiceConfigurationNotFound(_logger, _serviceName, configPath);
        }
        else
        {
            Log.ConfiguredEndpoints(_logger, _serviceName, configPath, endpoints.Endpoints, resolvedEndpointCount);
        }

        return default;
    }

    string IHostNameFeature.HostName => _serviceName;

    private void AddEndpoint(List<ServiceEndpoint> endpoints, IConfigurationSection section, string endpointName)
    {
        var value = section.Value;
        if (string.IsNullOrWhiteSpace(value) || !TryParseEndPoint(value, out var endPoint))
        {
            throw new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' endpoint '{endpointName}' has an invalid value with key '{section.Key}'.");
        }

        endpoints.Add(CreateEndpoint(endPoint));
    }

    private static bool TryParseEndPoint(string value, [NotNullWhen(true)] out EndPoint? endPoint)
    {
        if (value.IndexOf("://") < 0 && Uri.TryCreate($"fakescheme://{value}", default, out var uri))
        {
            var port = uri.Port > 0 ? uri.Port : 0;
            if (IPAddress.TryParse(uri.Host, out var ip))
            {
                endPoint = new IPEndPoint(ip, port);
            }
            else
            {
                endPoint = new DnsEndPoint(uri.Host, port);
            }
        }
        else if (Uri.TryCreate(value, default, out uri))
        {
            endPoint = new UriEndPoint(uri);
        }
        else
        {
            endPoint = null;
            return false;
        }

        return true;
    }

    private ServiceEndpoint CreateEndpoint(EndPoint endPoint)
    {
        var serviceEndpoint = ServiceEndpoint.Create(endPoint);
        serviceEndpoint.Features.Set<IServiceEndpointProvider>(this);
        if (_options.Value.ShouldApplyHostNameMetadata(serviceEndpoint))
        {
            serviceEndpoint.Features.Set<IHostNameFeature>(this);
        }

        return serviceEndpoint;
    }

    public override string ToString() => "Configuration";
}
