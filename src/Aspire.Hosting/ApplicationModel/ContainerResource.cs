#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPROBES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a specified container.
/// </summary>
public class ContainerResource : Resource, IResourceWithEnvironment, IResourceWithArgs, IResourceWithEndpoints, IResourceWithWaitSupport, IResourceWithProbes,
    IComputeResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="entrypoint">An optional container entrypoint.</param>
    public ContainerResource(string name, string? entrypoint = null) : base(name)
    {
        Entrypoint = entrypoint;
    }

    /// <summary>
    /// The container Entrypoint.
    /// </summary>
    /// <remarks><c>null</c> means use the default Entrypoint defined by the container.</remarks>
    public string? Entrypoint { get; set; }
}
