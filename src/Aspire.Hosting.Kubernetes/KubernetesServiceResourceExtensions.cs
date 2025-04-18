// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using static Aspire.Hosting.Kubernetes.KubernetesResource;

namespace Aspire.Hosting.Kubernetes;

internal static class KubernetesServiceResourceExtensions
{
    internal static async Task<object> ProcessValueAsync(this KubernetesResource resource, KubernetesEnvironmentContext context, DistributedApplicationExecutionContext executionContext, object value)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var referencedResource = ep.Resource == resource
                    ? resource
                    : await context.CreateKubernetesResourceAsync(ep.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[ep.EndpointName];

                var url = GetEndpointValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param, resource.TargetResource);
            }

            if (value is ConnectionStringReference cs)
            {
                value = cs.Resource.ConnectionStringExpression;
                continue;
            }

            if (value is IResourceWithConnectionString csrs)
            {
                value = csrs.ConnectionStringExpression;
                continue;
            }

            if (value is EndpointReferenceExpression epExpr)
            {
                var referencedResource = epExpr.Endpoint.Resource == resource
                    ? resource
                    : await context.CreateKubernetesResourceAsync(epExpr.Endpoint.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetEndpointValue(mapping, epExpr.Property);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                if (expr is { Format: "{0}", ValueProviders.Count: 1 })
                {
                    return (await resource.ProcessValueAsync(context, executionContext, expr.ValueProviders[0]).ConfigureAwait(false)).ToString() ?? string.Empty;
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = await resource.ProcessValueAsync(context, executionContext, vp).ConfigureAwait(false);
                    args[index++] = val ?? throw new InvalidOperationException("Value is null");
                }

                return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
            }

            // If we don't know how to process the value, we just return it as an external reference
            if (value is IManifestExpressionProvider r)
            {
                context.Logger.NotSupportedResourceWarning(nameof(value), r.GetType().Name);

                return ResolveUnknownValue(r, resource.TargetResource);
            }

            throw new NotSupportedException($"Unsupported value type: {value.GetType().Name}");
        }
    }

    private static string GetEndpointValue(EndpointMapping mapping, EndpointProperty property)
    {
        var (scheme, host, port, _, _) = mapping;

        return property switch
        {
            EndpointProperty.Url => GetHostValue($"{scheme}://", suffix: $":{port}"),
            EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
            EndpointProperty.Port => port,
            EndpointProperty.HostAndPort => GetHostValue(suffix: $":{port}"),
            EndpointProperty.TargetPort => port,
            EndpointProperty.Scheme => scheme,
            _ => throw new NotSupportedException(),
        };

        string GetHostValue(string? prefix = null, string? suffix = null)
        {
            return $"{prefix}{host}{suffix}";
        }
    }

    private static HelmExpressionWithValue AllocateParameter(ParameterResource parameter, IResource resource)
    {
        var formattedName = parameter.Name.ToManifestFriendlyResourceName();

        var expression = parameter.Secret ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        var value = parameter.Default is null || parameter.Secret ? null : parameter.Value;
        return new(expression, value);
    }

    private static HelmExpressionWithValue ResolveUnknownValue(IManifestExpressionProvider parameter, IResource resource)
    {
        var formattedName = parameter.ValueExpression.Replace("{", "")
            .Replace("}", "")
            .Replace(".", "_")
            .ToManifestFriendlyResourceName();

        var helmExpression = parameter.ValueExpression.ContainsHelmSecretExpression() ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        return new(helmExpression, parameter.ValueExpression);
    }
}
