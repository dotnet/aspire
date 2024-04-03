// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Describes a query for endpoints of a service.
/// </summary>
public sealed class ServiceEndPointQuery
{
    /// <summary>
    /// Initializes a new <see cref="ServiceEndPointQuery"/> instance.
    /// </summary>
    /// <param name="originalString">The string which the query was constructed from.</param>
    /// <param name="includedSchemes">The ordered list of included URI schemes.</param>
    /// <param name="serviceName">The service name.</param>
    /// <param name="endPointName">The optional endpoint name.</param>
    private ServiceEndPointQuery(string originalString, string[] includedSchemes, string serviceName, string? endPointName)
    {
        OriginalString = originalString;
        IncludeSchemes = includedSchemes;
        ServiceName = serviceName;
        EndPointName = endPointName;
    }

    /// <summary>
    /// Tries to parse the provided input as a service endpoint query.
    /// </summary>
    /// <param name="input">The value to parse.</param>
    /// <param name="query">The resulting query.</param>
    /// <returns><see langword="true"/> if the value was successfully parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string input, [NotNullWhen(true)] out ServiceEndPointQuery? query)
    {
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
        string? endPointName = null;
        if (uriHost.StartsWith('_') && segmentSeparatorIndex > 1 && uriHost[^1] != '.')
        {
            endPointName = uriHost[1..segmentSeparatorIndex];

            // Skip the endpoint name, including its prefix ('_') and suffix ('.').
            host = uriHost[(segmentSeparatorIndex + 1)..];
        }
        else
        {
            host = uriHost;
        }

        // Allow multiple schemes to be separated by a '+', eg. "https+http://host:port".
        var schemes = hasScheme ? uri.Scheme.Split('+') : [];
        query = new(input, schemes, host, endPointName);
        return true;
    }

    /// <summary>
    /// Gets the string which the query was constructed from.
    /// </summary>
    public string OriginalString { get; }

    /// <summary>
    /// Gets the ordered list of included URI schemes.
    /// </summary>
    public IReadOnlyList<string> IncludeSchemes { get; }

    /// <summary>
    /// Gets the endpoint name, or <see langword="null"/> if no endpoint name is specified.
    /// </summary>
    public string? EndPointName { get; }

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; }

    /// <inheritdoc/>
    public override string? ToString() => EndPointName is not null ? $"Service: {ServiceName}, Endpoint: {EndPointName}, Schemes: {string.Join(", ", IncludeSchemes)}" : $"Service: {ServiceName}, Schemes: {string.Join(", ", IncludeSchemes)}";
}

