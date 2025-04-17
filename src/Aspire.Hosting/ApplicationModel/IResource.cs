// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that can be hosted by an application.
/// </summary>
public interface IResource
{
    /// <summary>
    /// Gets the name of the resource.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the annotations associated with the resource.
    /// </summary>
    ResourceAnnotationCollection Annotations { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="visitor"></param>
    Task AcceptAsync(IResourceVisitor visitor)
    {
        return Task.CompletedTask;
    }
}
