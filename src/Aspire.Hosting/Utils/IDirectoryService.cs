// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Service for managing Aspire directories.
/// </summary>
/// <remarks>
/// This service provides a centralized way to manage directories used by Aspire,
/// including temporary files, cache, and other storage needs. Directory locations
/// can be overridden using environment variables or configuration.
/// </remarks>
public interface IDirectoryService
{
    /// <summary>
    /// Gets the temporary directory service for managing temporary files and directories.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The temp directory defaults to ~/.aspire/temp but can be overridden via:
    /// <list type="bullet">
    /// <item>ASPIRE_TEMP_FOLDER environment variable</item>
    /// <item>Aspire:TempDirectory configuration setting</item>
    /// </list>
    /// </para>
    /// <para>
    /// Use this instead of <see cref="Path.GetTempPath"/>, <see cref="Path.GetTempFileName"/>,
    /// or <see cref="Directory.CreateTempSubdirectory(string?)"/> to ensure consistent temp file management.
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
    /// Gets the base path for all Aspire temporary files.
    /// </summary>
    /// <remarks>
    /// By default, this is ~/.aspire/temp, but can be overridden via:
    /// <list type="bullet">
    /// <item>ASPIRE_TEMP_FOLDER environment variable</item>
    /// <item>Aspire:TempDirectory configuration setting</item>
    /// </list>
    /// </remarks>
    string BasePath { get; }

    /// <summary>
    /// Creates and returns the path to a temporary subdirectory.
    /// </summary>
    /// <param name="prefix">Optional prefix for the subdirectory name.</param>
    /// <returns>The full path to the created temporary subdirectory.</returns>
    /// <remarks>
    /// The subdirectory will be created if it doesn't exist.
    /// This method is thread-safe.
    /// </remarks>
    string CreateSubdirectory(string? prefix = null);

    /// <summary>
    /// Gets the path to a temporary file within the Aspire temp directory.
    /// </summary>
    /// <param name="extension">Optional file extension (including the dot, e.g., ".json").</param>
    /// <returns>The full path to a unique temporary file.</returns>
    /// <remarks>
    /// The file is not created by this method, only the path is returned.
    /// The caller is responsible for creating and managing the file.
    /// </remarks>
    string GetFilePath(string? extension = null);

    /// <summary>
    /// Creates a subdirectory within the Aspire temp directory and returns its path.
    /// </summary>
    /// <param name="subdirectory">The subdirectory path relative to the base temp directory.</param>
    /// <returns>The full path to the created subdirectory.</returns>
    /// <remarks>
    /// The directory is created if it doesn't exist.
    /// This method is thread-safe.
    /// </remarks>
    string CreateSubdirectoryPath(string subdirectory);
}
