// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREFILESYSTEM001 // Type is for evaluation purposes only

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting;

/// <summary>
/// Default implementation of <see cref="IFileSystemService"/>.
/// </summary>
internal sealed class FileSystemService : IFileSystemService, IDisposable
{
    private readonly TempFileSystemService _tempDirectory;
    private ILogger<FileSystemService>? _logger;
    private readonly bool _preserveTempFiles;

    public FileSystemService()
    {
        // Check environment variable to preserve temp files for debugging
        _preserveTempFiles = Environment.GetEnvironmentVariable("ASPIRE_PRESERVE_TEMP_FILES") is not null;
        
        _tempDirectory = new TempFileSystemService(this);
    }

    /// <summary>
    /// Sets the logger for this service. Called after service provider is built.
    /// </summary>
    /// <remarks>
    /// The logger cannot be injected via constructor because the FileSystemService
    /// is allocated before logging is fully initialized in the DistributedApplicationBuilder.
    /// </remarks>
    internal void SetLogger(ILogger<FileSystemService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public ITempFileSystemService TempDirectory => _tempDirectory;

    // Track allocated temp files and directories
    private readonly ConcurrentDictionary<string, bool> _allocatedPaths = new();

    internal void TrackAllocatedPath(string path, bool isDirectory)
    {
        _allocatedPaths.TryAdd(path, isDirectory);
        
        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug("Allocated temporary {Type}: {Path}", isDirectory ? "directory" : "file", path);
        }
    }

    internal void UntrackPath(string path)
    {
        _allocatedPaths.TryRemove(path, out _);
    }

    internal bool ShouldPreserveTempFiles() => _preserveTempFiles;

    /// <summary>
    /// Cleans up any remaining temporary files and directories.
    /// </summary>
    public void Dispose()
    {
        if (_preserveTempFiles)
        {
            _logger?.LogInformation("Skipping cleanup of {Count} temporary files/directories due to ASPIRE_PRESERVE_TEMP_FILES environment variable", _allocatedPaths.Count);
            return;
        }

        if (_allocatedPaths.IsEmpty)
        {
            return;
        }

        _logger?.LogDebug("Cleaning up {Count} remaining temporary files/directories", _allocatedPaths.Count);

        foreach (var kvp in _allocatedPaths)
        {
            var path = kvp.Key;
            var isDirectory = kvp.Value;

            try
            {
                if (isDirectory)
                {
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        _logger?.LogDebug("Cleaned up temporary directory: {Path}", path);
                    }
                }
                else
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        _logger?.LogDebug("Cleaned up temporary file: {Path}", path);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to clean up temporary {Type}: {Path}", isDirectory ? "directory" : "file", path);
            }
        }

        _allocatedPaths.Clear();
    }

    /// <summary>
    /// Implementation of <see cref="ITempFileSystemService"/>.
    /// </summary>
    private sealed class TempFileSystemService : ITempFileSystemService
    {
        private readonly FileSystemService _parent;

        public TempFileSystemService(FileSystemService parent)
        {
            _parent = parent;
        }

        /// <inheritdoc/>
        public TempDirectory CreateTempSubdirectory(string? prefix = null)
        {
            var path = Directory.CreateTempSubdirectory(prefix ?? "aspire").FullName;
            _parent.TrackAllocatedPath(path, isDirectory: true);
            return new DefaultTempDirectory(path, _parent);
        }

        /// <inheritdoc/>
        public TempFile CreateTempFile(string? fileName = null)
        {
            if (fileName is null)
            {
                var tempFile = Path.GetTempFileName();
                _parent.TrackAllocatedPath(tempFile, isDirectory: false);
                return new DefaultTempFile(tempFile, deleteParentDirectory: false, _parent);
            }

            // Create a temp subdirectory and place the named file inside it
            var tempDir = Directory.CreateTempSubdirectory("aspire").FullName;
            var filePath = Path.Combine(tempDir, fileName);
            File.Create(filePath).Dispose();
            _parent.TrackAllocatedPath(filePath, isDirectory: false);
            return new DefaultTempFile(filePath, deleteParentDirectory: true, _parent);
        }
    }

    /// <summary>
    /// Default implementation of <see cref="TempDirectory"/>.
    /// </summary>
    private sealed class DefaultTempDirectory : TempDirectory
    {
        private readonly string _path;
        private readonly FileSystemService _parent;
        private bool _disposed;

        public DefaultTempDirectory(string path, FileSystemService parent)
        {
            _path = path;
            _parent = parent;
        }

        public override string Path => _path;

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _parent.UntrackPath(_path);

            // Skip deletion if preserve flag is set
            if (_parent.ShouldPreserveTempFiles())
            {
                return;
            }

            try
            {
                if (Directory.Exists(_path))
                {
                    Directory.Delete(_path, recursive: true);
                }
            }
            catch
            {
                // Ignore errors during cleanup
            }
        }
    }

    /// <summary>
    /// Default implementation of <see cref="TempFile"/>.
    /// </summary>
    private sealed class DefaultTempFile : TempFile
    {
        private readonly string _path;
        private readonly bool _deleteParentDirectory;
        private readonly FileSystemService _parent;
        private bool _disposed;

        public DefaultTempFile(string path, bool deleteParentDirectory, FileSystemService parent)
        {
            _path = path;
            _deleteParentDirectory = deleteParentDirectory;
            _parent = parent;
        }

        public override string Path => _path;

        public override void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _parent.UntrackPath(_path);

            // Skip deletion if preserve flag is set
            if (_parent.ShouldPreserveTempFiles())
            {
                return;
            }

            try
            {
                if (File.Exists(_path))
                {
                    File.Delete(_path);
                }

                if (_deleteParentDirectory)
                {
                    var parentDir = System.IO.Path.GetDirectoryName(_path);
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
    }
}
