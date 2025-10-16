// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a URL on the host machine. When referenced in a container resource, localhost will be
/// replaced with the configured container host name.
/// </summary>
public record HostUrl(string Url) : IValueProvider, IManifestExpressionProvider
{
    // Goes into the manifest as a value, not an expression
    string IManifestExpressionProvider.ValueExpression => Url;

    // Returns the url
    ValueTask<string?> IValueProvider.GetValueAsync(System.Threading.CancellationToken _) => GetNetworkValueAsync(null);

    // Returns the url
    ValueTask<string?> IValueProvider.GetValueAsync(ValueProviderContext context, CancellationToken _)
    {
        return context.Network switch
        {
            NetworkIdentifier networkContext => GetNetworkValueAsync(networkContext),
            _ => GetNetworkValueAsync(null)
        };
    }

    private ValueTask<string?> GetNetworkValueAsync(NetworkIdentifier? context)
    {
        // HostUrl is a bit of a hack that is not modeled as an expression
        // So in this one case, we need to fix up the container host name 'manually'
        // Internally, this is only used for OTEL_EXPORTER_OTLP_ENDPOINT, but HostUrl
        // is public, so we don't control how it is used

        if (context is null || context == KnownNetworkIdentifiers.LocalhostNetwork)
        {
            return new(Url);
        }

        var retval = Url;

        try
        {
            var uri = new UriBuilder(Url);
            if (uri.Host is "localhost" or "127.0.0.1" or "[::1]")
            {
                var hasEndingSlash = Url.EndsWith('/');
                uri.Host = KnownHostNames.DefaultContainerTunnelHostName;
                retval = uri.ToString();

                // Remove trailing slash if we didn't have one before (UriBuilder always adds one)
                if (!hasEndingSlash && retval.EndsWith('/'))
                {
                    retval = retval[..^1];
                }
            }
        }
        catch (UriFormatException)
        {
            // HostUrl was meant to only be used with valid URLs. However, this was not
            // previously enforced. So we need to handle the case where it's not a valid URL,
            // by falling back to a simple string replacement.
            retval = retval.Replace(KnownHostNames.Localhost, KnownHostNames.DefaultContainerTunnelHostName, StringComparison.OrdinalIgnoreCase)
                         .Replace("127.0.0.1", KnownHostNames.DefaultContainerTunnelHostName)
                         .Replace("[::1]", KnownHostNames.DefaultContainerTunnelHostName);
        }

        return new(retval);
    }
}
