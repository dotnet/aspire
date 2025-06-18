// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Extensions;
using static Aspire.Hosting.Kubernetes.KubernetesResource;

namespace Aspire.Hosting.Kubernetes;

internal static class KubernetesServiceResourceExtensions
{
    /// <summary>
    /// Creates a Helm value placeholder for the specified <see cref="IManifestExpressionProvider"/>.
    /// </summary>
    /// <param name="manifestExpressionProvider">The manifest expression provider.</param>
    /// <param name="kubernetesResource">The Kubernetes resource to associate the value with.</param>
    /// <param name="secret">Whether this should be placed in secrets vs values.</param>
    /// <returns>A string representing the Helm value placeholder.</returns>
    public static string AsHelmValuePlaceholder(this IManifestExpressionProvider manifestExpressionProvider, KubernetesResource kubernetesResource, bool secret = false)
    {
        var formattedName = manifestExpressionProvider.ValueExpression.Replace("{", "")
            .Replace("}", "")
            .Replace(".", "_")
            .ToHelmValuesSectionName();

        var expression = secret ?
            formattedName.ToHelmSecretExpression(kubernetesResource.TargetResource.Name) :
            formattedName.ToHelmConfigExpression(kubernetesResource.TargetResource.Name);

        if (secret)
        {
            kubernetesResource.Secrets[formattedName] = new(expression, manifestExpressionProvider.ValueExpression);
        }
        else
        {
            kubernetesResource.EnvironmentVariables[formattedName] = new(expression, manifestExpressionProvider.ValueExpression);
        }

        return expression;
    }

    /// <summary>
    /// Creates a Helm value placeholder for the specified <see cref="ParameterResource"/>.
    /// </summary>
    /// <param name="parameter">The parameter resource for which to create the Helm value placeholder.</param>
    /// <param name="kubernetesResource">The Kubernetes resource to associate the value with.</param>
    /// <returns>A string representing the Helm value placeholder.</returns>
    public static string AsHelmValuePlaceholder(this ParameterResource parameter, KubernetesResource kubernetesResource)
    {
        var formattedName = parameter.Name.ToHelmValuesSectionName();

        var expression = parameter.Secret ?
            formattedName.ToHelmSecretExpression(kubernetesResource.TargetResource.Name) :
            formattedName.ToHelmConfigExpression(kubernetesResource.TargetResource.Name);

        var value = parameter.Default is null || parameter.Secret ? null : parameter.Value;

        if (parameter.Secret)
        {
            kubernetesResource.Secrets[formattedName] = new(expression, value);
        }
        else
        {
            kubernetesResource.EnvironmentVariables[formattedName] = new(expression, value);
        }

        return expression;
    }

    internal static string AsHelmValuePlaceholder(this KubernetesResource kubernetesResource, bool secret = false)
    {
        var resourceInstance = kubernetesResource.TargetResource;

        var portKey = $"{resourceInstance.Name}_PORT".ToUpperInvariant().Replace("-", "_").ToHelmValuesSectionName();

        var expression = secret ?
            portKey.ToHelmSecretExpression(resourceInstance.Name) :
            portKey.ToHelmConfigExpression(resourceInstance.Name);

        const string defaultValue = "8080";

        if (secret)
        {
            kubernetesResource.Secrets[portKey] = new(expression, defaultValue);
        }
        else
        {
            kubernetesResource.EnvironmentVariables[portKey] = new(expression, defaultValue);
        }

        return expression;
    }

    internal static object ProcessValue(this KubernetesResource resource, object value)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var referencedResource = ep.Resource == resource.TargetResource
                    ? resource
                    : resource.Parent.ResourceMapping[ep.Resource];

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
                var referencedResource = epExpr.Endpoint.Resource == resource.TargetResource
                    ? resource
                    : resource.Parent.ResourceMapping[epExpr.Endpoint.Resource];

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetEndpointValue(mapping, epExpr.Property);

                return val;
            }

            if (value is ReferenceExpression expr)
            {
                if (expr is { Format: "{0}", ValueProviders.Count: 1 })
                {
                    return resource.ProcessValue(expr.ValueProviders[0]).ToString() ?? string.Empty;
                }

                var args = new object[expr.ValueProviders.Count];
                var index = 0;

                foreach (var vp in expr.ValueProviders)
                {
                    var val = resource.ProcessValue(vp);
                    args[index++] = val ?? throw new InvalidOperationException("Value is null");
                }

                return string.Format(CultureInfo.InvariantCulture, expr.Format, args);
            }

            // If we don't know how to process the value, we just return it as an external reference
            if (value is IManifestExpressionProvider r)
            {
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
        var formattedName = parameter.Name.ToHelmValuesSectionName();

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
            .ToHelmValuesSectionName();

        var helmExpression = parameter.ValueExpression.ContainsHelmSecretExpression() ?
            formattedName.ToHelmSecretExpression(resource.Name) :
            formattedName.ToHelmConfigExpression(resource.Name);

        return new(helmExpression, parameter.ValueExpression);
    }
}
