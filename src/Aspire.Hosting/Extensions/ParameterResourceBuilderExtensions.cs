// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Extensions;

public static class ParameterResourceBuilderExtensions
{
    public static IResourceBuilder<ParameterResource> AddParameter(this IDistributedApplicationBuilder builder, string name, string value)
    {
        var resource = new ParameterResource(name, value);
        return builder.AddResource(resource);
    }

    public static IResourceBuilder<IResourceWithConnectionString> AsConnectionString(this IResourceBuilder<ParameterResource> builder)
    {
        var resource = new ResourceWithConnectionStringSurrogate(builder.Resource, () => builder.Resource.Value);
        return new DistributedApplicationResourceBuilder<IResourceWithConnectionString>(builder.ApplicationBuilder, resource);
    }
}
