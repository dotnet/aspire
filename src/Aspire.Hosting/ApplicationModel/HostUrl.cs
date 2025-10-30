// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
    ValueTask<string?> IValueProvider.GetValueAsync(System.Threading.CancellationToken cancellationToken) => ((IValueProvider)this).GetValueAsync(new(), cancellationToken);

    // Returns the url
    async ValueTask<string?> IValueProvider.GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        var networkContext = context.Network ?? context.Caller?.GetDefaultResourceNetwork() ?? KnownNetworkIdentifiers.LocalhostNetwork;

        // HostUrl is a bit of a hack that is not modeled as an expression
        // So in this one case, we need to fix up the container host name 'manually'
        // Internally, this is only used for OTEL_EXPORTER_OTLP_ENDPOINT, but HostUrl
        // is public, so we don't control how it is used

        if (networkContext == KnownNetworkIdentifiers.LocalhostNetwork)
        {
            return new(Url);
        }

        var retval = Url;

        try
        {
            var uri = new UriBuilder(Url);
            if (uri.Host is "localhost" or "127.0.0.1" or "[::1]")
            {
                if (context.ExecutionContext is { } && context.ExecutionContext.IsRunMode)
                {
                    var options = context.ExecutionContext.ServiceProvider.GetRequiredService<IOptions<DcpOptions>>();

                    var infoService = context.ExecutionContext.ServiceProvider.GetRequiredService<IDcpDependencyCheckService>();
                    var dcpInfo = await infoService.GetDcpInfoAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    var hasEndingSlash = Url.EndsWith('/');
                    uri.Host = options.Value.EnableAspireContainerTunnel == true ? KnownHostNames.DefaultContainerTunnelHostName : dcpInfo?.Containers?.ContainerHostName ?? KnownHostNames.DockerDesktopHostBridge;

                    // We need to consider that both the host and port may need to be remapped
                    var model = context.ExecutionContext.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                    var targetResource = model.Resources.FirstOrDefault(r =>
                    {
                        // Find a non-container resource with an endpoint matching the original localhost:port
                        return !r.IsContainer() &&
                            r is IResourceWithEndpoints &&
                            r.TryGetEndpoints(out var endpoints) &&
                            endpoints.Any(ep => ep.DefaultNetworkID == KnownNetworkIdentifiers.LocalhostNetwork && ep.Port == uri.Port);
                    });

                    if (targetResource is IResourceWithEndpoints resourceWithEndpoints)
                    {
                        var originalEndpoint = resourceWithEndpoints.GetEndpoints().FirstOrDefault(ep => ep.ContextNetworkID == KnownNetworkIdentifiers.LocalhostNetwork && ep.Port == uri.Port);
                        if (originalEndpoint is not null)
                        {
                            // Find the mapped endpoint for the target network context
                            var mappedEndpoint = resourceWithEndpoints.GetEndpoint(originalEndpoint.EndpointName, networkContext);
                            if (mappedEndpoint is not null)
                            {
                                // Update the port to the mapped port
                                uri.Port = mappedEndpoint.Port;
                            }
                        }
                    }

                    retval = uri.ToString();

                    // Remove trailing slash if we didn't have one before (UriBuilder always adds one)
                    if (!hasEndingSlash && retval.EndsWith('/'))
                    {
                        retval = retval[..^1];
                    }
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
