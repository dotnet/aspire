// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Service for managing Aspire directories.
/// </summary>
/// <remarks>
/// This service provides a centralized way to manage directories used by Aspire,
/// including temporary files, cache, and other storage needs.
/// </remarks>
public interface IDirectoryService
{
    /// <summary>
    /// Gets the temporary directory service for managing temporary files and directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this instead of calling <see cref="Directory.CreateTempSubdirectory(string?)"/> directly
    /// to ensure consistent temp file management and enable testability.
    /// </para>
    /// </remarks>
    ITempDirectoryService TempDirectory { get; }
}

/// <summary>
/// Service for managing temporary directories and files within Aspire.
/// </summary>
public interface ITempDirectoryService
{
    /// <summary>
    /// Creates and returns the path to a temporary subdirectory.
    /// </summary>
    /// <param name="prefix">Optional prefix for the subdirectory name (e.g., "aspire").</param>
    /// <returns>The full path to the created temporary subdirectory.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a unique temporary subdirectory using the system temp folder.
    /// The directory is created immediately and the path is returned.
    /// </para>
    /// <para>
    /// Use this instead of calling <see cref="Directory.CreateTempSubdirectory(string?)"/> directly.
    /// </para>
    /// </remarks>
    string CreateTempSubdirectory(string? prefix = null);

    /// <summary>
    /// Creates a new temporary file and returns the full path to the file.
    /// </summary>
    /// <param name="extension">Optional file extension including the dot (e.g., ".txt", ".json"). If null, uses the default .tmp extension.</param>
    /// <returns>The full path to the created temporary file.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a new temporary file using <see cref="Path.GetTempFileName"/>
    /// and optionally renames it with the specified extension.
    /// </para>
    /// <para>
    /// Use this instead of calling <see cref="Path.GetTempFileName"/> directly.
    /// </para>
    /// </remarks>
    string GetTempFileName(string? extension = null);

    /// <summary>
    /// Creates a new temporary file with the specified name in a temporary directory and returns the full path.
    /// </summary>
    /// <param name="prefix">Prefix for the temporary directory name.</param>
    /// <param name="fileName">The name for the temporary file (e.g., "config.json", "script.php").</param>
    /// <returns>The full path to the created temporary file.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a temporary subdirectory with the given prefix and places a file with the specified name inside it.
    /// This is useful when the filename matters (e.g., for scripts that check their own filename).
    /// </para>
    /// </remarks>
    string CreateTempFile(string prefix, string fileName);
}
