// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// A service endpoint resolver that uses configuration to resolve endpoints.
/// </summary>
internal sealed partial class ConfigurationServiceEndPointResolver : IServiceEndPointResolver, IHostNameFeature
{
    private readonly string _serviceName;
    private readonly string? _endpointName;
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
    public ConfigurationServiceEndPointResolver(
        string serviceName,
        IConfiguration configuration,
        ILogger<ConfigurationServiceEndPointResolver> logger,
        IOptions<ConfigurationServiceEndPointResolverOptions> options)
    {
        if (ServiceNameParts.TryParse(serviceName, out var parts))
        {
            _serviceName = parts.Host;
            _endpointName = parts.EndPointName;
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
    public string DisplayName => "Configuration";

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => default;

    /// <inheritdoc/>
    public ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken) => new(ResolveInternal(endPoints));

    string IHostNameFeature.HostName => _serviceName;

    private ResolutionStatus ResolveInternal(ServiceEndPointCollectionSource endPoints)
    {
        // Only add endpoints to the collection if a previous provider (eg, an override) did not add them.
        if (endPoints.EndPoints.Count != 0)
        {
            Log.SkippedResolution(_logger, _serviceName, "Collection has existing endpoints");
            return ResolutionStatus.None;
        }

        var root = _configuration;
        var baseSectionName = _options.Value.SectionName;
        if (baseSectionName is { Length: > 0 })
        {
            root = root.GetSection(baseSectionName);
        }

        // Get the corresponding config section.
        var section = root.GetSection(_serviceName);
        var configPath = GetConfigurationPath(baseSectionName);
        Log.UsingConfigurationPath(_logger, configPath, _serviceName);
        if (!section.Exists())
        {
            return CreateNotFoundResponse(endPoints, configPath);
        }

        // Read the endpoint from the configuration.
        // First check if there is a collection of sections
        var children = section.GetChildren();
        if (children.Any())
        {
            var values = children.Select(c => c.Value!).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (values is { Count: > 0 })
            {
                // Use endpoint names if any of the values have an endpoint name set.
                var parsedValues = ParseServiceNameParts(values, configPath);
                Log.ConfiguredEndPoints(_logger, _serviceName, configPath, parsedValues);

                var matchEndPointNames = !parsedValues.TrueForAll(static uri => string.IsNullOrEmpty(uri.EndPointName));
                Log.EndPointNameMatchSelection(_logger, _serviceName, matchEndPointNames);

                foreach (var uri in parsedValues)
                {
                    // If either endpoint names are not in-use or the scheme matches, create an endpoint for this value.
                    if (!matchEndPointNames || EndPointNamesMatch(_endpointName, uri))
                    {
                        if (!ServiceNameParts.TryCreateEndPoint(uri, out var endPoint))
                        {
                            return ResolutionStatus.FromException(new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' is invalid."));
                        }

                        endPoints.EndPoints.Add(CreateEndPoint(endPoint));
                    }
                }
            }
        }
        else if (section.Value is { } value && ServiceNameParts.TryParse(value, out var parsed))
        {
            if (EndPointNamesMatch(_endpointName, parsed))
            {
                if (!ServiceNameParts.TryCreateEndPoint(parsed, out var endPoint))
                {
                    return ResolutionStatus.FromException(new KeyNotFoundException($"The endpoint configuration section for service '{_serviceName}' is invalid."));
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    Log.ConfiguredEndPoints(_logger, _serviceName, configPath, [parsed]);
                }

                endPoints.EndPoints.Add(CreateEndPoint(endPoint));
            }
        }

        if (endPoints.EndPoints.Count == 0)
        {
            Log.ConfigurationNotFound(_logger, _serviceName, configPath);
        }

        endPoints.AddChangeToken(section.GetReloadToken());
        return ResolutionStatus.Success;

        static bool EndPointNamesMatch(string? endPointName, ServiceNameParts parts) =>
            string.IsNullOrEmpty(parts.EndPointName)
            || string.IsNullOrEmpty(endPointName)
            || MemoryExtensions.Equals(parts.EndPointName, endPointName, StringComparison.OrdinalIgnoreCase);
    }

    private ServiceEndPoint CreateEndPoint(EndPoint endPoint)
    {
        var serviceEndPoint = ServiceEndPoint.Create(endPoint);
        serviceEndPoint.Features.Set<IServiceEndPointResolver>(this);
        if (_options.Value.ApplyHostNameMetadata(serviceEndPoint))
        {
            serviceEndPoint.Features.Set<IHostNameFeature>(this);
        }

        return serviceEndPoint;
    }

    private ResolutionStatus CreateNotFoundResponse(ServiceEndPointCollectionSource endPoints, string configPath)
    {
        endPoints.AddChangeToken(_configuration.GetReloadToken());
        Log.ConfigurationNotFound(_logger, _serviceName, configPath);
        return ResolutionStatus.CreateNotFound($"No configuration for the specified path '{configPath}' was found.");
    }

    private string GetConfigurationPath(string? baseSectionName)
    {
        var configPath = new StringBuilder();
        if (baseSectionName is { Length: > 0 })
        {
            configPath.Append(baseSectionName).Append(':');
        }

        configPath.Append(_serviceName);
        return configPath.ToString();
    }

    private List<ServiceNameParts> ParseServiceNameParts(List<string> input, string configPath)
    {
        var results = new List<ServiceNameParts>(input.Count);
        for (var i = 0; i < input.Count; ++i)
        {
            if (ServiceNameParts.TryParse(input[i], out var value))
            {
                if (!results.Contains(value))
                {
                    results.Add(value);
                }
            }
            else
            {
                throw new InvalidOperationException($"The endpoint configuration '{input[i]}' from path '{configPath}[{i}]' for service '{_serviceName}' is invalid.");
            }
        }

        return results;
    }

    public override string ToString() => "Configuration";
}
