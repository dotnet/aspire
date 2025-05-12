// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;

internal static class DockerComposeServiceResourceExtensions
{
    internal static object ProcessValue(this DockerComposeServiceResource resource, object value)
    {
        while (true)
        {
            if (value is string s)
            {
                return s;
            }

            if (value is EndpointReference ep)
            {
                var referencedResource = resource.Parent.ResourceMapping[ep.Resource];

                var mapping = referencedResource.EndpointMappings[ep.EndpointName];

                var url = GetValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return param.AsEnvironmentPlaceholder(resource);
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
                var referencedResource = resource.Parent.ResourceMapping[epExpr.Endpoint.Resource];

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetValue(mapping, epExpr.Property);

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
                return r.AsEnvironmentPlaceholder(resource);
            }

            return value; // todo: we need to never get here really...
        }
    }

    private static string GetValue(DockerComposeServiceResource.EndpointMapping mapping, EndpointProperty property)
    {
        return property switch
        {
            EndpointProperty.Url => GetHostValue($"{mapping.Scheme}://", suffix: mapping.IsHttpIngress ? null : $":{mapping.InternalPort}"),
            EndpointProperty.Host or EndpointProperty.IPV4Host => GetHostValue(),
            EndpointProperty.Port => mapping.InternalPort.ToString(CultureInfo.InvariantCulture),
            EndpointProperty.HostAndPort => GetHostValue(suffix: $":{mapping.InternalPort}"),
            EndpointProperty.TargetPort => $"{mapping.InternalPort}",
            EndpointProperty.Scheme => mapping.Scheme,
            _ => throw new NotSupportedException(),
        };

        string GetHostValue(string? prefix = null, string? suffix = null)
        {
            return $"{prefix}{mapping.Host}{suffix}";
        }
    }
}
