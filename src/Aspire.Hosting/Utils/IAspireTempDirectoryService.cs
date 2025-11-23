// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Utils;

/// <summary>
/// Service for managing Aspire temporary directories.
/// </summary>
/// <remarks>
/// This service provides a centralized way to manage temporary directories for Aspire,
/// consolidating temp storage into ~/.aspire/temp by default. The location can be
/// overridden using the ASPIRE_TEMP_FOLDER environment variable or configuration.
/// </remarks>
public interface IAspireTempDirectoryService
{
    /// <summary>
    /// Gets the base path for all Aspire temporary files.
    /// </summary>
    /// <remarks>
    /// By default, this is ~/.aspire/temp, but can be overridden via:
    /// - ASPIRE_TEMP_FOLDER environment variable
    /// - Aspire:TempDirectory configuration setting
    /// </remarks>
    string BaseTempDirectory { get; }

    /// <summary>
    /// Creates and returns the path to a temporary subdirectory.
    /// </summary>
    /// <param name="prefix">Optional prefix for the subdirectory name.</param>
    /// <returns>The full path to the created temporary subdirectory.</returns>
    /// <remarks>
    /// The subdirectory will be created if it doesn't exist.
    /// This method is thread-safe.
    /// </remarks>
    string CreateTempSubdirectory(string? prefix = null);

    /// <summary>
    /// Gets the path to a temporary file within the Aspire temp directory.
    /// </summary>
    /// <param name="extension">Optional file extension (including the dot, e.g., ".json").</param>
    /// <returns>The full path to a unique temporary file.</returns>
    /// <remarks>
    /// The file is not created by this method, only the path is returned.
    /// The caller is responsible for creating and managing the file.
    /// </remarks>
    string GetTempFilePath(string? extension = null);

    /// <summary>
    /// Gets the path to a subdirectory within the Aspire temp directory without creating it.
    /// </summary>
    /// <param name="subdirectory">The subdirectory path relative to the base temp directory.</param>
    /// <returns>The full path to the subdirectory.</returns>
    /// <remarks>
    /// The directory is not created by this method.
    /// Use this when you want to check for existence or manage the directory lifecycle manually.
    /// </remarks>
    string GetTempSubdirectoryPath(string subdirectory);
}
