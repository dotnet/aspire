// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

internal class ExpressionResolver
{
    /// <summary>
    /// Resolve an expression when it is being used from inside a container.
    /// So it's either a container-to-container or container-to-exe communication.
    /// </summary>
    static async ValueTask<string?> ResolveWithContainerSourceAsync(object? value, Dictionary<EndpointReference, bool[]> processedEndpoints, bool preprocessMode, CancellationToken cancellationToken)
    {
        async Task<string?> EvalEndpointAsync(EndpointReference endpointReference, EndpointProperty property)
        {
            if (preprocessMode)
            {
                if (!processedEndpoints.TryGetValue(endpointReference, out var hostAndPortFound))
                {
                    hostAndPortFound = new bool[2];
                    processedEndpoints[endpointReference] = hostAndPortFound;
                }

                if (property is EndpointProperty.Host or EndpointProperty.IPV4Host)
                {
                    hostAndPortFound[0] = true;
                }
                else if (property == EndpointProperty.Port)
                {
                    hostAndPortFound[1] = true;
                }
                else if (property == EndpointProperty.Url)
                {
                    hostAndPortFound[0] = hostAndPortFound[1] = true;
                }

                return string.Empty;
            }

            // We need to use the root resource, e.g. AzureStorageResource instead of AzureBlobResource
            // Otherwise, we get the wrong values for IsContainer and Name
            var target = endpointReference.Resource.GetRootResource();

            bool HasBothHostAndPort() => processedEndpoints[endpointReference][0] && processedEndpoints[endpointReference][1];

            return (property, target.IsContainer()) switch
            {
                // If Container -> Container, we go directly to the container name, bypassing the host
                (EndpointProperty.Host or EndpointProperty.IPV4Host, true) when HasBothHostAndPort() => target.Name,
                // If Container -> Container, we use the target port, since we're not going through the host.
                // But only do this if we have also processed the host property for that same endpoint.
                // This allows the host and port to be handled in a unified way.
                (EndpointProperty.Port, true) when HasBothHostAndPort() => await endpointReference.Property(EndpointProperty.TargetPort).GetValueAsync(cancellationToken).ConfigureAwait(false),
                // If Container -> Exe, we need to go through the container host
                (EndpointProperty.Host or EndpointProperty.IPV4Host, false) => endpointReference.ContainerHost,
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
                args[i] = await ResolveWithContainerSourceAsync(expr.ValueProviders[i], processedEndpoints, preprocessMode, cancellationToken).ConfigureAwait(false);
            }

            return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
        }

        return value switch
        {
            ConnectionStringReference cs => await ResolveWithContainerSourceAsync(cs.Resource.ConnectionStringExpression, processedEndpoints, preprocessMode, cancellationToken).ConfigureAwait(false),
            IResourceWithConnectionString cs => await ResolveWithContainerSourceAsync(cs.ConnectionStringExpression, processedEndpoints, preprocessMode, cancellationToken).ConfigureAwait(false),
            ReferenceExpression ex => await EvalExpressionAsync(ex).ConfigureAwait(false),
            EndpointReference endpointReference => await EvalEndpointAsync(endpointReference, EndpointProperty.Url).ConfigureAwait(false),
            EndpointReferenceExpression ep => await EvalEndpointAsync(ep.Endpoint, ep.Property).ConfigureAwait(false),
            IValueProvider vp => await vp.GetValueAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new NotImplementedException()
        };
    }

    internal static async ValueTask<string?> ResolveAsync(bool sourceIsContainer, IValueProvider valueProvider, CancellationToken cancellationToken)
    {
        var processedEndpoints = new Dictionary<EndpointReference, bool[]>();

        await ResolveWithContainerSourceAsync(valueProvider, processedEndpoints, preprocessMode: true, cancellationToken).ConfigureAwait(false);

        return sourceIsContainer switch
        {
            // Exe -> Exe and Exe -> Container cases
            false => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
            // Container -> Exe and Container -> Container cases
            true => await ResolveWithContainerSourceAsync(valueProvider, processedEndpoints, preprocessMode: false, cancellationToken).ConfigureAwait(false)
        };
    }
}
