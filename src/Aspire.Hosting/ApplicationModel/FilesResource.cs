// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a files resource that can be used by an application.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="files">The collection of file paths associated with this resource.</param>
public class FilesResource(string name, IEnumerable<string> files) : Resource(name), IResourceWithFiles, IResourceWithoutLifetime
{
    private readonly List<string> _files = files?.ToList() ?? [];

    /// <summary>
    /// Gets the collection of file paths associated with this resource.
    /// </summary>
    public IEnumerable<string> Files => _files;

    /// <summary>
    /// Adds a file path to the resource.
    /// </summary>
    /// <param name="filePath">The file path to add.</param>
    public void AddFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        _files.Add(filePath);
    }

    /// <summary>
    /// Adds multiple file paths to the resource.
    /// </summary>
    /// <param name="filePaths">The file paths to add.</param>
    public void AddFiles(IEnumerable<string> filePaths)
    {
        ArgumentNullException.ThrowIfNull(filePaths);
        _files.AddRange(filePaths);
    }
}