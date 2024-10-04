// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

internal class ExpressionResolver(string containerHostName, CancellationToken cancellationToken)
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
        // In the preprocess phase, our only goal is to determine if the host and port properties are both used
        // for each endpoint.
        if (Preprocess)
        {
            if (!_endpointUsage.TryGetValue(EndpointUniqueName(endpointReference), out var hostAndPortPresence))
            {
                hostAndPortPresence = new HostAndPortPresence();
                _endpointUsage[EndpointUniqueName(endpointReference)] = hostAndPortPresence;
            }

            if (property is EndpointProperty.Host or EndpointProperty.IPV4Host)
            {
                hostAndPortPresence.HasHost = true;
            }
            else if (property == EndpointProperty.Port)
            {
                hostAndPortPresence.HasPort = true;
            }
            else if (property == EndpointProperty.Url)
            {
                hostAndPortPresence.HasHost = hostAndPortPresence.HasPort = true;
            }

            return string.Empty;
        }

        // We need to use the root resource, e.g. AzureStorageResource instead of AzureBlobResource
        // Otherwise, we get the wrong values for IsContainer and Name
        var target = endpointReference.Resource.GetRootResource();

        bool HasBothHostAndPort() =>
            _endpointUsage[EndpointUniqueName(endpointReference)].HasHost &&
            _endpointUsage[EndpointUniqueName(endpointReference)].HasPort;

        return (property, target.IsContainer()) switch
        {
            // If Container -> Container, we go directly to the container name and target port, bypassing the host
            // But only do this if we have processed both the host and port properties for that same endpoint.
            // This allows the host and port to be handled in a unified way.
            (EndpointProperty.Host or EndpointProperty.IPV4Host, true) when HasBothHostAndPort() => target.Name,
            (EndpointProperty.Port, true) when HasBothHostAndPort() => await endpointReference.Property(EndpointProperty.TargetPort).GetValueAsync(cancellationToken).ConfigureAwait(false),
            // If Container -> Exe, we need to go through the container host
            (EndpointProperty.Host or EndpointProperty.IPV4Host, false) => containerHostName,
            (EndpointProperty.Url, _) => string.Format(CultureInfo.InvariantCulture, "{0}://{1}:{2}",
                                            endpointReference.Scheme,
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Host).ConfigureAwait(false),
                                            await EvalEndpointAsync(endpointReference, EndpointProperty.Port).ConfigureAwait(false)),
            _ => await endpointReference.Property(property).GetValueAsync(cancellationToken).ConfigureAwait(false)
        };
    }

    async Task<string?> EvalExpressionAsync(ReferenceExpression expr)
    {
        // This logic is similar to ReferenceExpression.GetValueAsync, except that we recurse on
        // our own resolver method
        var args = new object?[expr.ValueProviders.Count];

        for (var i = 0; i < expr.ValueProviders.Count; i++)
        {
            args[i] = await ResolveWithContainerSourceAsync(expr.ValueProviders[i]).ConfigureAwait(false);
        }

        return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
    }

    /// <summary>
    /// Resolve an expression when it is being used from inside a container.
    /// So it's either a container-to-container or container-to-exe communication.
    /// </summary>
    async ValueTask<string?> ResolveWithContainerSourceAsync(object? value)
    {
        return value switch
        {
            ConnectionStringReference cs => await ResolveWithContainerSourceAsync(cs.Resource.ConnectionStringExpression).ConfigureAwait(false),
            IResourceWithConnectionString cs => await ResolveWithContainerSourceAsync(cs.ConnectionStringExpression).ConfigureAwait(false),
            ReferenceExpression ex => await EvalExpressionAsync(ex).ConfigureAwait(false),
            EndpointReference endpointReference => await EvalEndpointAsync(endpointReference, EndpointProperty.Url).ConfigureAwait(false),
            EndpointReferenceExpression ep => await EvalEndpointAsync(ep.Endpoint, ep.Property).ConfigureAwait(false),
            IValueProvider vp => await vp.GetValueAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new NotImplementedException()
        };
    }

    internal static async ValueTask<string?> ResolveAsync(bool sourceIsContainer, IValueProvider valueProvider, string containerHostName, CancellationToken cancellationToken)
    {
        var resolver = new ExpressionResolver(containerHostName, cancellationToken);

        // Run the processing phase to know if the host and port properties are both used for each endpoint.
        resolver.Preprocess = true;
        await resolver.ResolveWithContainerSourceAsync(valueProvider).ConfigureAwait(false);
        resolver.Preprocess = false;

        return sourceIsContainer switch
        {
            // Exe -> Exe and Exe -> Container cases
            false => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
            // Container -> Exe and Container -> Container cases
            true => await resolver.ResolveWithContainerSourceAsync(valueProvider).ConfigureAwait(false)
        };
    }
}
