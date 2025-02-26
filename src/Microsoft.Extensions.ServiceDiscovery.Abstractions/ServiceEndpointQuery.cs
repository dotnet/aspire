// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Describes a query for endpoints of a service.
/// </summary>
public sealed class ServiceEndpointQuery
{
    private readonly string _originalString;

    /// <summary>
    /// Initializes a new <see cref="ServiceEndpointQuery"/> instance.
    /// </summary>
    /// <param name="originalString">The string which the query was constructed from.</param>
    /// <param name="includedSchemes">The ordered list of included URI schemes.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="endpointName">The optional endpoint name.</param>
    private ServiceEndpointQuery(string originalString, string[] includedSchemes, string serviceName, string? endpointName)
    {
        _originalString = originalString;
        IncludedSchemes = includedSchemes;
        ServiceName = serviceName;
        EndpointName = endpointName;
    }

    /// <summary>
    /// Tries to parse the provided input as a service endpoint query.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="query">The resulting query.</param>
    /// <returns><see langword="true"/> if the value was successfully parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string input, [NotNullWhen(true)] out ServiceEndpointQuery? query)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        bool hasScheme;
        if (!input.Contains("://", StringComparison.InvariantCulture)
            && Uri.TryCreate($"fakescheme://{input}", default, out var uri))
        {
            hasScheme = false;
        }
        else if (Uri.TryCreate(input, default, out uri))
        {
            hasScheme = true;
        }
        else
        {
            query = null;
            return false;
        }

        var uriHost = uri.Host;
        var segmentSeparatorIndex = uriHost.IndexOf('.');
        string host;
        string? endpointName = null;
        if (uriHost.StartsWith('_') && segmentSeparatorIndex > 1 && uriHost[^1] != '.')
        {
            endpointName = uriHost[1..segmentSeparatorIndex];

            // Skip the endpoint name, including its prefix ('_') and suffix ('.').
            host = uriHost[(segmentSeparatorIndex + 1)..];
        }
        else
        {
            host = uriHost;
        }

        // Allow multiple schemes to be separated by a '+', eg. "https+http://host:port".
        var schemes = hasScheme ? uri.Scheme.Split('+') : [];
        query = new(input, schemes, host, endpointName);
        return true;
    }

    /// <summary>
    /// Gets the ordered list of included URI schemes.
    /// </summary>
    public IReadOnlyList<string> IncludedSchemes { get; }

    /// <summary>
    /// Gets the endpoint name, or <see langword="null"/> if no endpoint name is specified.
    /// </summary>
    public string? EndpointName { get; }

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; }

    /// <inheritdoc/>
    public override string? ToString() => _originalString;
}

