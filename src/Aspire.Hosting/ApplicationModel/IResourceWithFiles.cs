// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a resource that can work with files.
/// </summary>
public interface IResourceWithFiles
{
    /// <summary>
    /// Gets the collection of file paths associated with this resource.
    /// </summary>
    IEnumerable<string> Files { get; }
}