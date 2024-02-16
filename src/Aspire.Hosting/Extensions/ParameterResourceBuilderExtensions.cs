// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding parameter resources to an application.
/// </summary>
public static class ParameterResourceBuilderExtensions
{
    /// <summary>
    /// Adds a parameter resource to the application.
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="secret">Optional flag indicating whether the parameter should be regarded as secret.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, bool secret = false)
    {
        return builder.AddParameter(name, () =>
        {
            var configurationKey = $"Parameters:{name}";
            return builder.Configuration[configurationKey] ?? throw new DistributedApplicationException($"Parameter resource could not be used because configuration key `{configurationKey}` is missing.");
        }, secret: false);
    }

    internal static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, Func<string> callback, bool secret = false)
    {
        var resource = new ParameterResource(name, callback, secret);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(context => WriteParameterResourceToManifest(context, resource));
    }

    private static void WriteParameterResourceToManifest(ManifestPublishingContext context, ParameterResource resource)
    {
        context.Writer.WriteString("type", "parameter.v0");
        context.Writer.WriteString("value", $"{{{resource.Name}.inputs.value}}");
        context.Writer.WriteStartObject("inputs");
        context.Writer.WriteStartObject("value");
        context.Writer.WriteString("type", "string");

        if (resource.Secret)
        {
            context.Writer.WriteBoolean("secret", resource.Secret);
        }

        context.Writer.WriteEndObject();
        context.Writer.WriteEndObject();
    }

    /// <summary>
    /// Adds a parameter to the distributed application but wrapped in a resource with a connection string for use with <see cref="ResourceBuilderExtensions.WithReference{TDestination}(IResourceBuilder{TDestination}, IResourceBuilder{IResourceWithConnectionString}, string?, bool)"/>
    /// </summary>
    /// <param name="builder">Distributed application builder</param>
    /// <param name="name">Name of parameter resource</param>
    /// <param name="environmentVariableName">Environment variable name to set when WithReference is used.</param>
    /// <returns>Resource builder for the parameter.</returns>
    /// <exception cref="DistributedApplicationException"></exception>
    public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, string name, string? environmentVariableName = null)
    {
        var parameterBuilder = builder.AddParameter(name, () =>
        {
            return builder.Configuration.GetConnectionString(name) ?? throw new DistributedApplicationException($"Connection string parameter resource could not be used because connection string `{name}` is missing.");
        },
        secret: true);

        var surrogate = new ResourceWithConnectionStringSurrogate(parameterBuilder.Resource, () => parameterBuilder.Resource.Value, environmentVariableName);
        return builder.CreateResourceBuilder(surrogate);
    }
}
