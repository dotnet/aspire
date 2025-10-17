// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Annotation that specifies which connection information should be injected into environment variables
/// when a resource is referenced using <c>WithReference()</c>.
/// </summary>
public sealed class ReferenceEnvironmentInjectionAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReferenceEnvironmentInjectionAnnotation"/>.
    /// </summary>
    /// <param name="flags">The flags specifying which connection information to inject.</param>
    public ReferenceEnvironmentInjectionAnnotation(ReferenceEnvironmentInjectionFlags flags)
    {
        Flags = flags;
    }

    /// <summary>
    /// Gets the flags specifying which connection information should be injected.
    /// </summary>
    public ReferenceEnvironmentInjectionFlags Flags { get; }
}
