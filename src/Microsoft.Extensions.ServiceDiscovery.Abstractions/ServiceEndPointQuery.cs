// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Describes a query for endpoints of a service.
/// </summary>
public class ServiceEndPointQuery
{
    /// <summary>
    /// The value indicating that all endpoint schemes are allowed.
    /// </summary>
#pragma warning disable IDE0300 // Simplify collection initialization
#pragma warning disable CA1825 // Avoid zero-length array allocations
    public static readonly string[] AllowAllSchemes = new string[0];
#pragma warning restore CA1825 // Avoid zero-length array allocations
#pragma warning restore IDE0300 // Simplify collection initialization

    /// <summary>
    /// Initializes a new <see cref="ServiceEndPointQuery"/> instance.
    /// </summary>
    /// <param name="originalString">The string which the query was constructed from.</param>
    /// <param name="includedSchemes">The ordered list of included URI schemes.</param>
    /// <param name="host">The host name.</param>
    /// <param name="endPointName">The optional endpoint name.</param>
    private ServiceEndPointQuery(string originalString, string[] includedSchemes, string host, string? endPointName)
    {
        OriginalString = originalString;
        IncludeSchemes = includedSchemes;
        Host = host;
        EndPointName = endPointName;
    }

    /// <summary>
    /// Tries to parse the provided input as a service endpoint query.
    /// </summary>
    /// <param name="queryString">The query string.</param>
    /// <param name="allowedSchemes">The allowed URI schemes. If the value is <see cref="AllowAllSchemes"/>, all schemes are allowed.</param>
    /// <param name="query">The resulting query.</param>
    /// <returns><see langword="true"/> if the value was successfully parsed; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string queryString, string[] allowedSchemes, [NotNullWhen(true)] out ServiceEndPointQuery? query)
    {
        bool hasScheme;
        if (!queryString.Contains("://", StringComparison.InvariantCulture)
            && Uri.TryCreate($"fakescheme://{queryString}", default, out var uri))
        {
            hasScheme = false;
        }
        else if (Uri.TryCreate(queryString, default, out uri))
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
        var schemes = hasScheme ? ParseSchemes(uri.Scheme, allowedSchemes) : [];

        query = new(queryString, schemes, host, endPointName);
        return true;

        static string[] ParseSchemes(string scheme, string[] allowedSchemes)
        {
            if (allowedSchemes.Equals(AllowAllSchemes))
            {
                return scheme.Split('+');
            }

            List<string> result = [];
            foreach (var s in scheme.Split('+'))
            {
                foreach (var allowed in allowedSchemes)
                {
                    if (string.Equals(s, allowed, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(s);
                        break;
                    }
                }
            }

            return result.ToArray();
        }
    }

    /// <summary>
    /// Gets the string which the query was constructed from.
    /// </summary>
    public string OriginalString { get; }

    /// <summary>
    /// Gets the collection of included URI schemes.
    /// </summary>
    public string[] IncludeSchemes { get; }

    /// <summary>
    /// Gets the endpoint name, or <see langword="null"/> if no endpoint name is specified.
    /// </summary>
    public string? EndPointName { get; }

    /// <summary>
    /// Gets the host name.
    /// </summary>
    public string Host { get; }

    /// <inheritdoc/>
    public override string? ToString() => EndPointName is not null ? $"EndPointName: {EndPointName}, Host: {Host}" : $"Host: {Host}";
}

