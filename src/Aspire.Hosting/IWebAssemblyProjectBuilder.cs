// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A builder for referencing Blazor WebAssembly (client) applications from Blazor Server applications in distributed (Aspire) applications.
/// </summary>
/// <typeparam name="TProject">A type that represents the project reference. It should be a Blazor WebAssembly (i.e. client) project.</typeparam>
public interface IWebAssemblyProjectBuilder<TProject> where TProject : IProjectMetadata, new()
{
    /// <summary>
    /// Injects service discovery information into the Blazor WebAssembly application from the project resource into the destination resource, using the source resource's name as the service name.
    /// </summary>
    /// <param name="source">The resource from which to extract service discovery information.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{ProjectResource}"/>.</returns>
    IResourceBuilder<ProjectResource> WithReference(IResourceBuilder<IResourceWithServiceDiscovery> source);
}
