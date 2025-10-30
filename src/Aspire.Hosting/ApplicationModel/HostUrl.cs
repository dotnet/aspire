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
        var networkContext = context.GetNetworkIdentifier();

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
                if (context.ExecutionContext?.IsRunMode == true)
                {
                    var options = context.ExecutionContext.ServiceProvider.GetRequiredService<IOptions<DcpOptions>>();

                    var infoService = context.ExecutionContext.ServiceProvider.GetRequiredService<IDcpDependencyCheckService>();
                    var dcpInfo = await infoService.GetDcpInfoAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    uri.Host = options.Value.EnableAspireContainerTunnel? KnownHostNames.DefaultContainerTunnelHostName : dcpInfo?.Containers?.ContainerHostName ?? KnownHostNames.DockerDesktopHostBridge;

                    if (options.Value.EnableAspireContainerTunnel)
                    {
                        // We need to consider that both the host and port may need to be remapped
                        var model = context.ExecutionContext.ServiceProvider.GetRequiredService<DistributedApplicationModel>();
                        var targetEndpoint = model.Resources.Where(r => !r.IsContainer())
                            .OfType<IResourceWithEndpoints>()
                            .Select(r =>
                            {
                                if (r.GetEndpoints(KnownNetworkIdentifiers.LocalhostNetwork).FirstOrDefault(ep => ep.Port == uri.Port) is EndpointReference ep)
                                {
                                    return r.GetEndpoint(ep.EndpointName, networkContext);
                                }

                                return null;
                            })
                            .Where(ep => ep is not null)
                            .FirstOrDefault();

                        if (targetEndpoint is { })
                        {
                            uri.Port = targetEndpoint.Port;
                        }
                    }

                    retval = uri.ToString();
                }
            }

            var hasEndingSlash = Url.EndsWith('/');

            // Remove trailing slash if we didn't have one before (UriBuilder always adds one)
            if (!hasEndingSlash && retval.EndsWith('/'))
            {
                retval = retval[..^1];
            }
        }
        catch (UriFormatException)
        {
            var replacementHost = KnownHostNames.DockerDesktopHostBridge;
            if (context.ExecutionContext?.IsRunMode == true)
            {
                var options = context.ExecutionContext.ServiceProvider.GetRequiredService<IOptions<DcpOptions>>();

                var infoService = context.ExecutionContext.ServiceProvider.GetRequiredService<IDcpDependencyCheckService>();
                var dcpInfo = await infoService.GetDcpInfoAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                replacementHost = options.Value.EnableAspireContainerTunnel ? KnownHostNames.DefaultContainerTunnelHostName : dcpInfo?.Containers?.ContainerHostName ?? KnownHostNames.DockerDesktopHostBridge;
            }

            // HostUrl was meant to only be used with valid URLs. However, this was not
            // previously enforced. So we need to handle the case where it's not a valid URL,
            // by falling back to a simple string replacement.
            retval = retval.Replace(KnownHostNames.Localhost, replacementHost, StringComparison.OrdinalIgnoreCase)
                         .Replace("127.0.0.1", replacementHost)
                         .Replace("[::1]", replacementHost);
        }

        return new(retval);
    }
}
