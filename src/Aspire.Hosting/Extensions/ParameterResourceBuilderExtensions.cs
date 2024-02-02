// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

public static class ParameterResourceBuilderExtensions
{
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

    public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, string name)
    {
        var parameterBuilder = builder.AddParameter(name, () =>
        {
            return builder.Configuration.GetConnectionString(name) ?? throw new DistributedApplicationException($"Connection string parameter resource could not be used because connection string `{name}` is missing.");
        }, secret: true);

        var surrogate = new ResourceWithConnectionStringSurrogate(parameterBuilder.Resource, () => parameterBuilder.Resource.Value);
        return new DistributedApplicationResourceBuilder<IResourceWithConnectionString>(builder, surrogate);
    }
}
