// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A .NET Aspire resource that is a Seq server.
/// </summary>
/// <param name="name">The name of the Seq resource</param>
public class SeqResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    /// <summary>
    /// Gets the Uri of the Seq endpoint
    /// </summary>
    public string GetConnectionString()
    {
        if (!this.TryGetAnnotationsOfType<AllocatedEndpointAnnotation>(out var seqEndpointAnnotations))
        {
            throw new DistributedApplicationException("Seq resource does not have endpoint annotation.");
        }

        return seqEndpointAnnotations.Single().UriString;
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.WriteContainer(this);

        context.Writer.WriteString(                     // "connectionString": "...",
            "connectionString",
            $"{{{Name}.bindings.tcp.host}}:{{{Name}.bindings.tcp.port}}");
    }
}
