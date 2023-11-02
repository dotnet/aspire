// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an abstract resource that can be used by an application, that implements <see cref="IResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
[DebuggerDisplay("{DebuggerToString(),nq}")]
public abstract class Resource(string name) : IResource
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    public string Name { get; } = name;
    
    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    public ResourceMetadataCollection Annotations { get; } = new();

    private string DebuggerToString()
    {
        return $@"Type = {GetType().Name}, Name = ""{Name}""";
    }
}
