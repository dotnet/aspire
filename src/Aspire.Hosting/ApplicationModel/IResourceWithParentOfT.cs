// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that has a parent resource of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of the parent resource.</typeparam>
public interface IResourceWithParent<out T> : IResource where T : IResource
{
    /// <summary>
    /// Gets the parent resource of type <typeparamref name="T"/>.
    /// </summary>
    T Parent { get; }
}
