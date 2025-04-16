// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker;

internal static class CommandLineArgsExtensions
{
    internal static async Task<object> ProcessValueAsync(this DockerComposeServiceResource resource, DockerComposeInfrastructure.DockerComposeEnvironmentContext context, DistributedApplicationExecutionContext executionContext, object value)
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
                    : await context.CreateDockerComposeServiceResourceAsync(ep.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[ep.EndpointName];

                var url = GetValue(mapping, EndpointProperty.Url);

                return url;
            }

            if (value is ParameterResource param)
            {
                return AllocateParameter(param, context);
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
                    : await context.CreateDockerComposeServiceResourceAsync(epExpr.Endpoint.Resource, executionContext, default).ConfigureAwait(false);

                var mapping = referencedResource.EndpointMappings[epExpr.Endpoint.EndpointName];

                var val = GetValue(mapping, epExpr.Property);

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
                return ResolveUnknownValue(r, resource);
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

    private static string ResolveParameterValue(ParameterResource parameter, DockerComposeInfrastructure.DockerComposeEnvironmentContext context)
    {
        // Placeholder for resolving the actual parameter value
        // https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#interpolation-syntax

        // Treat secrets as environment variable placeholders as for now
        // this doesn't handle generation of parameter values with defaults
        var env = parameter.Name.ToUpperInvariant().Replace("-", "_");

        context.AddEnv(env, $"Parameter {parameter.Name}",
                                            parameter.Secret || parameter.Default is null ? null : parameter.Value);

        return $"${{{env}}}";
    }

    private static string AllocateParameter(ParameterResource parameter, DockerComposeInfrastructure.DockerComposeEnvironmentContext context)
    {
        return ResolveParameterValue(parameter, context);
    }

    private static string ResolveUnknownValue(IManifestExpressionProvider parameter, DockerComposeServiceResource serviceResource)
    {
        // Placeholder for resolving the actual parameter value
        // https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#interpolation-syntax

        // Treat secrets as environment variable placeholders as for now
        // this doesn't handle generation of parameter values with defaults
        var env = parameter.ValueExpression.Replace("{", "")
                 .Replace("}", "")
                 .Replace(".", "_")
                 .Replace("-", "_")
                 .ToUpperInvariant();

        serviceResource.EnvironmentVariables.Add(env, $"Unknown reference {parameter.ValueExpression}");

        return $"${{{env}}}";
    }
}
