// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Extensions;

public static class ParameterResourceBuilderExtensions
{
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name)
    {
        return builder.AddParameter(name, () =>
        {
            var configurationKey = $"Parameters:{name}";
            return builder.Configuration[configurationKey] ?? throw new DistributedApplicationException($"Parameter resource could not be used because configuration key `{configurationKey}` is missing.");
        });
    }

    internal static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, Func<string> callback)
    {
        var resource = new ParameterResource(name, callback);
        return builder.AddResource(resource);
    }

    public static IResourceBuilder<IResourceWithConnectionString> AddConnectionString(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new ParameterResource(name, () =>
        {
            var configurationKey = $"ConnectionStrings:{name}";
            return builder.Configuration[configurationKey] ?? throw new DistributedApplicationException($"Connection string parameter resource could not be used because configuration key `{configurationKey}` is missing.");
        });

        builder.AddResource(resource); // Discard the builder because we'll return a surrogate.
        var surrogate = new ResourceWithConnectionStringSurrogate(resource, () => resource.Value);

        return new DistributedApplicationResourceBuilder<IResourceWithConnectionString>(builder, surrogate);
    }
}
