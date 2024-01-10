// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

public static class VolumeResourceBuilderExtensions
{
    /// <summary>
    /// Adds a volume to the application model that can be referenced by container workloads.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> instance.</param>
    /// <param name="name">The name of the container</param>
    /// <returns></returns>
    public static IResourceBuilder<VolumeResource> AddVolume(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new VolumeResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteVolumeResourceToManifest);
    }

    private static void WriteVolumeResourceToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "volume.v0");
    }
}
