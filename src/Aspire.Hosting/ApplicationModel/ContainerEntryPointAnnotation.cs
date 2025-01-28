// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An annotation to specify the entrypoint for a container.
/// </summary>
public class ContainerEntryPointAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The entrypoint for the container.
    /// </summary>
    public string? Entrypoint { get; set; }
}
