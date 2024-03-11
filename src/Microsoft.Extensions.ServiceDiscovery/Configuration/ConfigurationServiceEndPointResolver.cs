// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// A service endpoint resolver that uses configuration to resolve resolved.
/// </summary>
internal sealed partial class ConfigurationServiceEndPointResolver : IServiceEndPointProvider, IHostNameFeature
{
    private const string DefaultEndPointName = "default";
    private readonly string _serviceName;
    private readonly string? _endpointName;
    private readonly string[] _schemes;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationServiceEndPointResolver> _logger;
    private readonly IOptions<ConfigurationServiceEndPointResolverOptions> _options;

    /// <summary>
    /// Initializes a new <see cref="ConfigurationServiceEndPointResolver"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The options.</param>
    /// <param name="parser">The service name parser.</param>
    public ConfigurationServiceEndPointResolver(
        string serviceName,
        IConfiguration configuration,
        ILogger<ConfigurationServiceEndPointResolver> logger,
        IOptions<ConfigurationServiceEndPointResolverOptions> options,
        ServiceNameParser parser)
    {
        if (parser.TryParse(serviceName, out var parts))
        {
            _serviceName = parts.Host;
            _endpointName = parts.EndPointName;
            _schemes = parts.Schemes;
        }
        else
        {
            throw new InvalidOperationException($"Service name '{serviceName}' is not valid.");
        }

        _configuration = configuration;
        _logger = logger;
        _options = options;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc/>
    public ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken) => new(ResolveInternal(endPoints));

    string IHostNameFeature.HostName => _serviceName;

    private ResolutionStatus ResolveInternal(ServiceEndPointCollectionSource endPoints)
    {
        // Only add resolved to the collection if a previous provider (eg, an override) did not add them.
        if (endPoints.EndPoints.Count != 0)
        {
            Log.SkippedResolution(_logger, _serviceName, "Collection has existing endpoints");
            return ResolutionStatus.None;
        }

        // Get the corresponding config section.
        var section = _configuration.GetSection(_options.Value.SectionName).GetSection(_serviceName);
        if (!section.Exists())
        {
            return CreateNotFoundResponse(endPoints, $"{_options.Value.SectionName}:{_serviceName}");
        }

        endPoints.AddChangeToken(section.GetReloadToken());

        // Find an appropriate configuration section based on the input.
        IConfigurationSection? namedSection = null;
        string endpointName;
        if (string.IsNullOrWhiteSpace(_endpointName))
        {
            if (_schemes.Length == 0)
            {
                // Use the section named "default".
                endpointName = DefaultEndPointName;
                namedSection = section.GetSection(endpointName);
            }
            else
            {
                // Set the ideal endpoint name for error messages.
                endpointName = _schemes[0];

                // Treat the scheme as the endpoint name and use the first section with a matching endpoint name which exists
                foreach (var scheme in _schemes)
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
            return CreateNotFoundResponse(endPoints, configPath);
        }

        List<ServiceEndPoint> resolved = [];
        Log.UsingConfigurationPath(_logger, configPath, endpointName, _serviceName);

        // Account for both the single and multi-value cases.
        if (!string.IsNullOrWhiteSpace(namedSection.Value))
        {
            // Single value case.
            if (!TryAddEndPoint(resolved, namedSection, endpointName, out var error))
            {
                return error;
            }
        }
        else
        {
            // Multiple value case.
            foreach (var child in namedSection.GetChildren())
            {
                if (!int.TryParse(child.Key, out _))
                {
                    return ResolutionStatus.FromException(new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' endpoint '{endpointName}' has non-numeric keys."));
                }

                if (!TryAddEndPoint(resolved, child, endpointName, out var error))
                {
                    return error;
                }
            }
        }

        // Filter the resolved endpoints to only include those which match the specified scheme.
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

        var added = 0;
        foreach (var ep in resolved)
        {
            if (ep.EndPoint is UriEndPoint uri && uri.Uri.Scheme is { } scheme)
            {
                var index = Array.IndexOf(_schemes, scheme);
                if (index >= 0 && index <= minIndex)
                {
                    ++added;
                    endPoints.EndPoints.Add(ep);
                }
            }
            else
            {
                ++added;
                endPoints.EndPoints.Add(ep);
            }
        }

        if (added == 0)
        {
            return CreateNotFoundResponse(endPoints, configPath);
        }

        return ResolutionStatus.Success;

    }

    private bool TryAddEndPoint(List<ServiceEndPoint> endPoints, IConfigurationSection section, string endpointName, out ResolutionStatus error)
    {
        var value = section.Value;
        if (string.IsNullOrWhiteSpace(value) || !TryParseEndPoint(value, out var endPoint))
        {
            error = ResolutionStatus.FromException(new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' endpoint '{endpointName}' has an invalid value with key '{section.Key}'."));
            return false;
        }

        endPoints.Add(CreateEndPoint(endPoint));
        error = default;
        return true;
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

    private ServiceEndPoint CreateEndPoint(EndPoint endPoint)
    {
        var serviceEndPoint = ServiceEndPoint.Create(endPoint);
        serviceEndPoint.Features.Set<IServiceEndPointProvider>(this);
        if (_options.Value.ApplyHostNameMetadata(serviceEndPoint))
        {
            serviceEndPoint.Features.Set<IHostNameFeature>(this);
        }

        return serviceEndPoint;
    }

    private ResolutionStatus CreateNotFoundResponse(ServiceEndPointCollectionSource endPoints, string configPath)
    {
        endPoints.AddChangeToken(_configuration.GetReloadToken());
        Log.ServiceConfigurationNotFound(_logger, _serviceName, configPath);
        return ResolutionStatus.CreateNotFound($"No valid endpoint configuration was found for service '{_serviceName}' from path '{configPath}'.");
    }

    public override string ToString() => "Configuration";
}
