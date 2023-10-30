// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
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
    private readonly IOptions<ConfigurationServiceEndPointResolverOptions> _options;

    /// <summary>
    /// Initializes a new <see cref="ConfigurationServiceEndPointResolver"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="options">The options.</param>
    public ConfigurationServiceEndPointResolver(
        string serviceName,
        IConfiguration configuration,
        IOptions<ConfigurationServiceEndPointResolverOptions> options)
    {
        if (ServiceNameParts.TryParse(serviceName, out var parts))
        {
            _serviceName = parts.Host;
            _endpointName = parts.EndPointName;
        }
        else
        {
            throw new InvalidOperationException($"Service name '{serviceName}' is not valid");
        }

        _configuration = configuration;
        _options = options;
    }

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
        if (!section.Exists())
        {
            return CreateNotFoundResponse(endPoints, baseSectionName);
        }

        // Read the endpoint from the configuration.
        // First check if there is a collection of sections
        var children = section.GetChildren();
        if (children.Any())
        {
            var values = children.Select(c => c.Value!).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (values is { Count: > 0 })
            {
                // Use schemes if any of the URIs have a scheme set.
                var uris = ParseServiceNameParts(values);
                var useSchemes = !uris.TrueForAll(static uri => string.IsNullOrEmpty(uri.EndPointName));
                foreach (var uri in uris)
                {
                    // If either schemes are not in-use or the scheme matches, create an endpoint for this value
                    if (!useSchemes || SchemesMatch(_endpointName, uri))
                    {
                        if (!ServiceNameParts.TryCreateEndPoint(uri, out var endPoint))
                        {
                            return ResolutionStatus.FromException(new KeyNotFoundException($"The configuration section for service endpoint {_serviceName} is invalid."));
                        }

                        endPoints.EndPoints.Add(CreateEndPoint(endPoint));
                    }
                }
            }
        }
        else if (section.Value is { } value && ServiceNameParts.TryParse(value, out var uri))
        {
            if (SchemesMatch(_endpointName, uri))
            {
                if (!ServiceNameParts.TryCreateEndPoint(uri, out var endPoint))
                {
                    return ResolutionStatus.FromException(new KeyNotFoundException($"The configuration section for service endpoint {_serviceName} is invalid."));
                }

                endPoints.EndPoints.Add(CreateEndPoint(endPoint));
            }
        }

        endPoints.AddChangeToken(section.GetReloadToken());
        return ResolutionStatus.Success;

        static bool SchemesMatch(string? scheme, ServiceNameParts parts) =>
            (string.IsNullOrEmpty(parts.EndPointName) || string.IsNullOrEmpty(scheme))
            || MemoryExtensions.Equals(parts.EndPointName, scheme, StringComparison.OrdinalIgnoreCase);
    }

    private ServiceEndPoint CreateEndPoint(EndPoint endPoint)
    {
        var serviceEndPoint = ServiceEndPoint.Create(endPoint);
        if (_options.Value.ApplyHostNameMetadata(serviceEndPoint))
        {
            serviceEndPoint.Features.Set<IHostNameFeature>(this);
        }

        return serviceEndPoint;
    }

    private ResolutionStatus CreateNotFoundResponse(ServiceEndPointCollectionSource endPoints, string? baseSectionName)
    {
        var configPath = new StringBuilder();
        if (baseSectionName is { Length: > 0 })
        {
            configPath.Append(baseSectionName).Append(':');
        }

        configPath.Append(_serviceName);
        endPoints.AddChangeToken(_configuration.GetReloadToken());
        return ResolutionStatus.CreateNotFound($"No configuration for the specified path \"{configPath}\" was found");
    }

    private static List<ServiceNameParts> ParseServiceNameParts(List<string> input)
    {
        var results = new List<ServiceNameParts>(input.Count);
        for (var i = 0; i < input.Count; ++i)
        {
            if (ServiceNameParts.TryParse(input[i], out var value))
            {
                results.Add(value);
            }
        }

        return results;
    }
}
