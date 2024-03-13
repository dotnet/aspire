// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Internal;

internal sealed class ServiceNameParser(IOptions<ServiceDiscoveryOptions> options)
{
    private readonly string[] _allowedSchemes = options.Value.AllowedSchemes;

    public bool TryParse(string serviceName, [NotNullWhen(true)] out ServiceNameParts parts)
    {
        if (serviceName.IndexOf("://") < 0 && Uri.TryCreate($"fakescheme://{serviceName}", default, out var uri))
        {
            parts = Create(uri, hasScheme: false);
            return true;
        }

        if (Uri.TryCreate(serviceName, default, out uri))
        {
            parts = Create(uri, hasScheme: true);
            return true;
        }

        parts = default;
        return false;

        ServiceNameParts Create(Uri uri, bool hasScheme)
        {
            var uriHost = uri.Host;
            var segmentSeparatorIndex = uriHost.IndexOf('.');
            string host;
            string? endPointName = null;
            var port = uri.Port > 0 ? uri.Port : 0;
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
            var schemes = hasScheme ? ParseSchemes(uri.Scheme) : [];
            return new(schemes, host, endPointName, port);
        }
    }

    private string[] ParseSchemes(string scheme)
    {
        if (_allowedSchemes.Equals(ServiceDiscoveryOptions.AllSchemes))
        {
            return scheme.Split('+');
        }

        List<string> result = [];
        foreach (var s in scheme.Split('+'))
        {
            foreach (var allowed in _allowedSchemes)
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

