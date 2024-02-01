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
            var configurationValue = builder.Configuration[configurationKey] ?? throw new DistributedApplicationException($"Parameter resource could not be used because configuration key `{configurationKey}` is missing.");
            return Task.FromResult(configurationValue);
        });
    }

    internal static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, Func<Task<string>> callback)
    {
        var resource = new ParameterResource(name, callback);
        return builder.AddResource(resource);
    }

    public static IResourceBuilder<IResourceWithConnectionString> AsConnectionString(this IResourceBuilder<ParameterResource> builder)
    {
        var resource = new ResourceWithConnectionStringSurrogate(builder.Resource, () => builder.Resource.Value);
        return new DistributedApplicationResourceBuilder<IResourceWithConnectionString>(builder.ApplicationBuilder, resource);
    }
}
