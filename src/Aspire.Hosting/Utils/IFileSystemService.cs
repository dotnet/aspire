// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting;

/// <summary>
/// Service for managing Aspire directories.
/// </summary>
/// <remarks>
/// This service provides a centralized way to manage directories used by Aspire,
/// including temporary files, cache, and other storage needs.
/// </remarks>
[Experimental("ASPIREFILESYSTEM001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IFileSystemService
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
    ITempFileSystemService TempDirectory { get; }
}

/// <summary>
/// Service for managing temporary directories and files within Aspire.
/// </summary>
[Experimental("ASPIREFILESYSTEM001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface ITempFileSystemService
{
    /// <summary>
    /// Creates and returns a temporary subdirectory.
    /// </summary>
    /// <param name="prefix">Optional prefix for the subdirectory name (e.g., "aspire").</param>
    /// <returns>A <see cref="TempDirectory"/> representing the created temporary subdirectory. Dispose to delete.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a unique temporary subdirectory using the system temp folder.
    /// The directory is created immediately. Dispose the returned object to delete the directory.
    /// </para>
    /// <para>
    /// Use this instead of calling <see cref="Directory.CreateTempSubdirectory(string?)"/> directly.
    /// </para>
    /// </remarks>
    TempDirectory CreateTempSubdirectory(string? prefix = null);

    /// <summary>
    /// Creates a new temporary file and returns it.
    /// </summary>
    /// <param name="extension">Optional file extension including the dot (e.g., ".txt", ".json"). If null, uses the default .tmp extension.</param>
    /// <returns>A <see cref="TempFile"/> representing the created temporary file. Dispose to delete.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a new temporary file using <see cref="Path.GetTempFileName"/>
    /// and optionally renames it with the specified extension. Dispose the returned object to delete the file.
    /// </para>
    /// <para>
    /// Use this instead of calling <see cref="Path.GetTempFileName"/> directly.
    /// </para>
    /// </remarks>
    TempFile GetTempFileName(string? extension = null);

    /// <summary>
    /// Creates a new temporary file with the specified name in a temporary directory.
    /// </summary>
    /// <param name="prefix">Prefix for the temporary directory name.</param>
    /// <param name="fileName">The name for the temporary file (e.g., "config.json", "script.php").</param>
    /// <returns>A <see cref="TempFile"/> representing the created temporary file. Dispose to delete the file and its parent directory.</returns>
    /// <remarks>
    /// <para>
    /// This method creates a temporary subdirectory with the given prefix and places a file with the specified name inside it.
    /// This is useful when the filename matters (e.g., for scripts that check their own filename).
    /// Dispose the returned object to delete the file and its parent directory.
    /// </para>
    /// </remarks>
    TempFile CreateTempFile(string prefix, string fileName);
}

/// <summary>
/// Represents a temporary directory that will be deleted when disposed.
/// </summary>
[Experimental("ASPIREFILESYSTEM001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class TempDirectory : IDisposable
{
    /// <summary>
    /// Gets the full path to the temporary directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempDirectory"/> class.
    /// </summary>
    /// <param name="path">The full path to the temporary directory.</param>
    public TempDirectory(string path)
    {
        Path = path;
    }

    /// <summary>
    /// Deletes the temporary directory and all its contents.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    /// <summary>
    /// Implicitly converts a <see cref="TempDirectory"/> to its path string.
    /// </summary>
    /// <param name="tempDirectory">The temporary directory.</param>
    public static implicit operator string(TempDirectory tempDirectory) => tempDirectory.Path;
}

/// <summary>
/// Represents a temporary file that will be deleted when disposed.
/// </summary>
[Experimental("ASPIREFILESYSTEM001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class TempFile : IDisposable
{
    private readonly bool _deleteParentDirectory;

    /// <summary>
    /// Gets the full path to the temporary file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TempFile"/> class.
    /// </summary>
    /// <param name="path">The full path to the temporary file.</param>
    /// <param name="deleteParentDirectory">Whether to delete the parent directory when disposed.</param>
    public TempFile(string path, bool deleteParentDirectory = false)
    {
        Path = path;
        _deleteParentDirectory = deleteParentDirectory;
    }

    /// <summary>
    /// Deletes the temporary file and optionally its parent directory.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }

            if (_deleteParentDirectory)
            {
                var parentDir = System.IO.Path.GetDirectoryName(Path);
                if (parentDir is not null && Directory.Exists(parentDir))
                {
                    Directory.Delete(parentDir, recursive: true);
                }
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }

    /// <summary>
    /// Implicitly converts a <see cref="TempFile"/> to its path string.
    /// </summary>
    /// <param name="tempFile">The temporary file.</param>
    public static implicit operator string(TempFile tempFile) => tempFile.Path;
}
