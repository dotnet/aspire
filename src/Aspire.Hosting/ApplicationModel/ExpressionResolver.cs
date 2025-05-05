// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

internal class ExpressionResolver(string containerHostName, CancellationToken cancellationToken, bool sourceIsContainer)
{
    class HostAndPortPresence
    {
        public bool HasHost { get; set; }
        public bool HasPort { get; set; }
    }

    // For each endpoint, keep track of whether host and port are in use
    // The key is the unique name of the endpoint, which is the resource name and endpoint name
    readonly Dictionary<string, HostAndPortPresence> _endpointUsage = [];
    static string EndpointUniqueName(EndpointReference endpointReference) => $"{endpointReference.Resource.Name}/{endpointReference.EndpointName}";

    // This marks whether we are in the preprocess phase or not
    // Not thread-safe, but we doesn't matter, since this class is never used concurrently
    bool Preprocess { get; set; }

    async Task<string?> EvalEndpointAsync(EndpointReference endpointReference, EndpointProperty property)
    {
        var endpointUniqueName = EndpointUniqueName(endpointReference);

        // In the preprocess phase, our only goal is to determine if the host and port properties are both used
        // for each endpoint.
        if (Preprocess)
        {
            if (!_endpointUsage.TryGetValue(endpointUniqueName, out var hostAndPortPresence))
            {
                hostAndPortPresence = new HostAndPortPresence();
                _endpointUsage[endpointUniqueName] = hostAndPortPresence;
            }

            if (property is EndpointProperty.Host or EndpointProperty.IPV4Host)
            {
                hostAndPortPresence.HasHost = true;
            }
            else if (property == EndpointProperty.Port)
            {
                hostAndPortPresence.HasPort = true;
            }
            else if (property is EndpointProperty.Url or EndpointProperty.HostAndPort)
            {
                hostAndPortPresence.HasHost = hostAndPortPresence.HasPort = true;
            }
            return string.Empty;
        }
        // We need to use the root resource, e.g. AzureStorageResource instead of AzureBlobResource
        // Otherwise, we get the wrong values for IsContainer and Name
        var target = endpointReference.Resource.GetRootResource();

        bool HasBothHostAndPort() =>
            _endpointUsage[endpointUniqueName].HasHost &&
            _endpointUsage[endpointUniqueName].HasPort;

        return (property, target.IsContainer(), HasBothHostAndPort()) switch
        {
            // If Container -> Container, we go directly to the container name and target port, bypassing the host
            // But only do this if we have processed both the host and port properties for that same endpoint.
            // This allows the host and port to be handled in a unified way.
            (EndpointProperty.Host or EndpointProperty.IPV4Host, true, true) => target.Name,
            (EndpointProperty.Port, true, true) => await endpointReference.Property(EndpointProperty.TargetPort).GetValueAsync(cancellationToken).ConfigureAwait(false),
            // If Container -> Exe, we need to go through the container host
            (EndpointProperty.Host or EndpointProperty.IPV4Host, false, _) => containerHostName,
            (EndpointProperty.Url, _, _) => string.Format(CultureInfo.InvariantCulture, "{0}://{1}:{2}",
                                            endpointReference.Scheme,
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Host).ConfigureAwait(false),
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Port).ConfigureAwait(false)),
            (EndpointProperty.HostAndPort, _, _) => string.Format(CultureInfo.InvariantCulture, "{0}:{1}",
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Host).ConfigureAwait(false),
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Port).ConfigureAwait(false)),
            _ => await endpointReference.Property(property).GetValueAsync(cancellationToken).ConfigureAwait(false)
        };
    }

    async Task<ResolvedValue> EvalExpressionAsync(ReferenceExpression expr)
    {
        // This logic is similar to ReferenceExpression.GetValueAsync, except that we recurse on
        // our own resolver method
        var args = new object?[expr.ValueProviders.Count];
        var isSensitive = false;

        for (var i = 0; i < expr.ValueProviders.Count; i++)
        {
            var result = await ResolveInternalAsync(expr.ValueProviders[i]).ConfigureAwait(false);
            args[i] = result?.Value;
            if (result?.IsSensitive is true)
            {
                isSensitive = true;
            }
        }

        // Identically to ReferenceExpression.GetValueAsync, we return null if the format is empty
        var value = expr.Format.Length == 0 ? null : string.Format(CultureInfo.InvariantCulture, expr.Format, args);

        return new ResolvedValue(value, isSensitive);
    }

    async Task<ResolvedValue> EvalValueProvider(IValueProvider vp)
    {
        var value = await vp.GetValueAsync(cancellationToken).ConfigureAwait(false);

        if (vp is ParameterResource pr)
        {
            return new ResolvedValue(value, pr.Secret);
        }

        // No need to do extra work, since the below will only be valid for containers.
        if (!sourceIsContainer)
        {
            return new ResolvedValue(value, false);
        }

        if (vp is HostUrl && value != null)
        {
            // HostUrl is a bit of a hack that is not modeled as an expression
            // So in this one case, we need to fix up the container host name 'manually'
            // Internally, this is only used for OTEL_EXPORTER_OTLP_ENDPOINT, but HostUrl
            // is public, so we don't control how it is used
            try
            {
                var uri = new UriBuilder(value);
                if (uri.Host is "localhost" or "127.0.0.1" or "[::1]")
                {
                    var hasEndingSlash = value.EndsWith('/');
                    uri.Host = containerHostName;
                    value = uri.ToString();

                    // Remove trailing slash if we didn't have one before (UriBuilder always adds one)
                    if (!hasEndingSlash && value.EndsWith('/'))
                    {
                        value = value[..^1];
                    }
                }
            }
            catch (UriFormatException)
            {
                // HostUrl was meant to only be used with valid URLs. However, this was not
                // previously enforced. So we need to handle the case where it's not a valid URL,
                // by falling back to a simple string replacement.
                value = value.Replace("localhost", containerHostName, StringComparison.OrdinalIgnoreCase)
                             .Replace("127.0.0.1", containerHostName)
                             .Replace("[::1]", containerHostName);
            }
        }

        return new ResolvedValue(value, false);
    }

    async Task<ResolvedValue> ResolveConnectionStringReferenceAsync(ConnectionStringReference cs)
    {
        // We are substituting our own logic for ConnectionStringReference's GetValueAsync.
        // However, ConnectionStringReference#GetValueAsync will throw if the connection string is not optional but is not present.
        // so we need to do the same here.
        var value = await ResolveInternalAsync(cs.Resource.ConnectionStringExpression).ConfigureAwait(false);

        // While pre-processing the endpoints, we never throw
        if (!Preprocess && string.IsNullOrEmpty(value.Value) && !cs.Optional)
        {
            cs.ThrowConnectionStringUnavailableException();
        }

        return value;
    }

    /// <summary>
    /// Resolve an expression. When it is being used from inside a container, endpoints may be evaluated (either in a container-to-container or container-to-exe communication).
    /// </summary>
    async ValueTask<ResolvedValue> ResolveInternalAsync(object? value)
    {
        return value switch
        {
            ConnectionStringReference cs => await ResolveConnectionStringReferenceAsync(cs).ConfigureAwait(false),
            IResourceWithConnectionString cs and not ConnectionStringParameterResource => await ResolveInternalAsync(cs.ConnectionStringExpression).ConfigureAwait(false),
            ReferenceExpression ex => await EvalExpressionAsync(ex).ConfigureAwait(false),
            EndpointReference endpointReference when sourceIsContainer => new ResolvedValue(await EvalEndpointAsync(endpointReference, EndpointProperty.Url).ConfigureAwait(false), false),
            EndpointReferenceExpression ep when sourceIsContainer => new ResolvedValue(await EvalEndpointAsync(ep.Endpoint, ep.Property).ConfigureAwait(false), false),
            IValueProvider vp => await EvalValueProvider(vp).ConfigureAwait(false),
            _ => throw new NotImplementedException()
        };
    }

    static async ValueTask<ResolvedValue> ResolveWithContainerSourceAsync(IValueProvider valueProvider, string containerHostName, bool sourceIsContainer, CancellationToken cancellationToken)
    {
        var resolver = new ExpressionResolver(containerHostName, cancellationToken, sourceIsContainer);

        // Run the processing phase to know if the host and port properties are both used for each endpoint.
        resolver.Preprocess = true;
        await resolver.ResolveInternalAsync(valueProvider).ConfigureAwait(false);
        resolver.Preprocess = false;

        return await resolver.ResolveInternalAsync(valueProvider).ConfigureAwait(false);
    }

    internal static async ValueTask<ResolvedValue> ResolveAsync(bool sourceIsContainer, IValueProvider valueProvider, string containerHostName, CancellationToken cancellationToken)
    {
        return await ResolveWithContainerSourceAsync(valueProvider, containerHostName, sourceIsContainer, cancellationToken).ConfigureAwait(false);
    }
}

internal record ResolvedValue(string? Value, bool IsSensitive);
