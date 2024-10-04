// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

internal class ExpressionResolver(string containerHostName)
{
    // For each endpoint, store two bools, to track if the host and port are in use
    readonly Dictionary<string, bool[]> _endpointUsage = [];

    /// <summary>
    /// Resolve an expression when it is being used from inside a container.
    /// So it's either a container-to-container or container-to-exe communication.
    /// </summary>
    /// <param name="value">The current object to be processed (or preprocessed)</param>
    /// <param name="preprocess">When true, we just prescan to determine if the host and port properties are both used</param>
    /// <param name="cancellationToken"></param>
    async ValueTask<string?> ResolveWithContainerSourceAsync(object? value, bool preprocess, CancellationToken cancellationToken)
    {
        async Task<string?> EvalEndpointAsync(EndpointReference endpointReference, EndpointProperty property)
        {
            // In the preprocess phase, our only goal is to determine if the host and port properties are both used
            // for each endpoint.
            if (preprocess)
            {
                if (!_endpointUsage.TryGetValue(endpointReference.EndpointName, out var hostAndPortPresence))
                {
                    hostAndPortPresence = new bool[2];
                    _endpointUsage[endpointReference.EndpointName] = hostAndPortPresence;
                }

                if (property is EndpointProperty.Host or EndpointProperty.IPV4Host)
                {
                    hostAndPortPresence[0] = true;
                }
                else if (property == EndpointProperty.Port)
                {
                    hostAndPortPresence[1] = true;
                }
                else if (property == EndpointProperty.Url)
                {
                    hostAndPortPresence[0] = hostAndPortPresence[1] = true;
                }

                return string.Empty;
            }

            // We need to use the root resource, e.g. AzureStorageResource instead of AzureBlobResource
            // Otherwise, we get the wrong values for IsContainer and Name
            var target = endpointReference.Resource.GetRootResource();

            bool HasBothHostAndPort() => _endpointUsage[endpointReference.EndpointName][0] && _endpointUsage[endpointReference.EndpointName][1];

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
                args[i] = await ResolveWithContainerSourceAsync(expr.ValueProviders[i], preprocess, cancellationToken).ConfigureAwait(false);
            }

            return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
        }

        return value switch
        {
            ConnectionStringReference cs => await ResolveWithContainerSourceAsync(cs.Resource.ConnectionStringExpression, preprocess, cancellationToken).ConfigureAwait(false),
            IResourceWithConnectionString cs => await ResolveWithContainerSourceAsync(cs.ConnectionStringExpression, preprocess, cancellationToken).ConfigureAwait(false),
            ReferenceExpression ex => await EvalExpressionAsync(ex).ConfigureAwait(false),
            EndpointReference endpointReference => await EvalEndpointAsync(endpointReference, EndpointProperty.Url).ConfigureAwait(false),
            EndpointReferenceExpression ep => await EvalEndpointAsync(ep.Endpoint, ep.Property).ConfigureAwait(false),
            IValueProvider vp => await vp.GetValueAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new NotImplementedException()
        };
    }

    internal static async ValueTask<string?> ResolveAsync(bool sourceIsContainer, IValueProvider valueProvider, string containerHostName, CancellationToken cancellationToken)
    {
        var resolver = new ExpressionResolver(containerHostName);

        // Run the processing phase to know if the host and port properties are both used for each endpoint.
        await resolver.ResolveWithContainerSourceAsync(valueProvider, preprocess: true, cancellationToken).ConfigureAwait(false);

        return sourceIsContainer switch
        {
            // Exe -> Exe and Exe -> Container cases
            false => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
            // Container -> Exe and Container -> Container cases
            true => await resolver.ResolveWithContainerSourceAsync(valueProvider, preprocess: false, cancellationToken).ConfigureAwait(false)
        };
    }
}
