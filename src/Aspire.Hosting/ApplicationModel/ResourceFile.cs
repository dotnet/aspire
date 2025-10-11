// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a file resource with both full and relative paths.
/// </summary>
[DebuggerDisplay("FullPath = {FullPath}, RelativePath = {RelativePath}")]
public sealed class ResourceFile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceFile"/> class.
    /// </summary>
    /// <param name="fullPath">The full path to the file on the local system.</param>
    /// <param name="relativePath">The normalized path relative to the root of the file set.</param>
    public ResourceFile(string fullPath, string relativePath)
    {
        ArgumentNullException.ThrowIfNull(fullPath);
        ArgumentNullException.ThrowIfNull(relativePath);
        
        FullPath = fullPath;
        RelativePath = relativePath;
    }

    /// <summary>
    /// Gets the full path to the file on the local system.
    /// </summary>
    public string FullPath { get; }

    /// <summary>
    /// Gets the normalized path relative to the root of the file set.
    /// </summary>
    public string RelativePath { get; }
}