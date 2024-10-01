// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Hosting.ApplicationModel;

internal class ExpressionResolver
{
    // Resolve an expression when it is being used from inside a container
    // This means that if the target is also a container, we're dealing with container-to-container communication
    static async ValueTask<string?> ResolveWithContainerSource(object? value, CancellationToken cancellationToken)
    {
        async Task<string?> EvalEndpoint(EndpointReference endpointReference, EndpointProperty property)
        {
            // We need to use the top resource, e.g. AzureStorageResource instead of AzureBlobResource
            // Otherwise, we get the wrong values for IsContainer and Name
            var target = endpointReference.Resource.GetRootResource();

            return (property, target.IsContainer()) switch
            {
                // If Container -> Container, we go directly to the container name, bypassing the host
                (EndpointProperty.Host or EndpointProperty.IPV4Host, true) => target.Name,
                // If Container -> Exe, we need to go through the container host
                (EndpointProperty.Host or EndpointProperty.IPV4Host, false) => endpointReference.ContainerHost,
                // If Container -> Container, we use the target port, since we're not going through the host
                (EndpointProperty.Port, true) => await endpointReference.Property(EndpointProperty.TargetPort).GetValueAsync(cancellationToken).ConfigureAwait(false),
                (EndpointProperty.Url, true) => $"{endpointReference.Scheme}://{target.Name}:{endpointReference.TargetPort}",
                _ => await endpointReference.Property(property).GetValueAsync(cancellationToken).ConfigureAwait(false)
            };
        }

        async Task<string?> EvalExpression(ReferenceExpression expr)
        {
            // This logic is similar to ReferenceExpression.GetValueAsync, except that we recurse on
            // our own resolver method
            var args = new object?[expr.ValueProviders.Count];

            for (var i = 0; i < expr.ValueProviders.Count; i++)
            {
                args[i] = await ResolveWithContainerSource(expr.ValueProviders[i], cancellationToken).ConfigureAwait(false);
            }

            return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
        }

        return value switch
        {
            ConnectionStringReference cs => await ResolveWithContainerSource(cs.Resource.ConnectionStringExpression, cancellationToken).ConfigureAwait(false),
            IResourceWithConnectionString cs => await ResolveWithContainerSource(cs.ConnectionStringExpression, cancellationToken).ConfigureAwait(false),
            ReferenceExpression ex => await EvalExpression(ex).ConfigureAwait(false),
            EndpointReference endpointReference => await EvalEndpoint(endpointReference, EndpointProperty.Url).ConfigureAwait(false),
            EndpointReferenceExpression ep => await EvalEndpoint(ep.Endpoint, ep.Property).ConfigureAwait(false),
            IValueProvider vp => await vp.GetValueAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new NotImplementedException()
        };
    }

    internal static async ValueTask<string?> Resolve(bool sourceIsContainer, IValueProvider valueProvider, CancellationToken cancellationToken) =>
        sourceIsContainer switch
        {
            // Exe -> Exe and Exe -> Container cases
            false => await valueProvider.GetValueAsync(cancellationToken).ConfigureAwait(false),
            // Container -> Exe and Container -> Container cases
            true => await ResolveWithContainerSource(valueProvider, cancellationToken).ConfigureAwait(false)
        };
}
