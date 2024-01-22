// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a container resource that implements <see cref="IResourceWithEnvironment"/> 
/// and <see cref="IResourceWithBindings"/>.
/// </summary>
public class ContainerResource(string name, string? entrypoint = null) : Resource(name), IResourceWithEnvironment, IResourceWithBindings
{
    /// <summary>
    /// The container Entrypoint.
    /// </summary>
    /// <remarks><c>null</c> means use the default Entrypoint defined by the container.</remarks>
    public string? Entrypoint { get; set; } = entrypoint;
}
