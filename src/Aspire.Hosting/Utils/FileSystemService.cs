// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Default implementation of <see cref="IFileSystemService"/>.
/// </summary>
internal sealed class FileSystemService : IFileSystemService
{
    private readonly TempFileSystemService _tempDirectory = new();

    /// <inheritdoc/>
    public ITempFileSystemService TempDirectory => _tempDirectory;

    /// <summary>
    /// Implementation of <see cref="ITempFileSystemService"/>.
    /// </summary>
    private sealed class TempFileSystemService : ITempFileSystemService
    {
        /// <inheritdoc/>
        public TempDirectory CreateTempSubdirectory(string? prefix = null)
        {
            var path = Directory.CreateTempSubdirectory(prefix ?? "aspire").FullName;
            return new DefaultTempDirectory(path);
        }

        /// <inheritdoc/>
        public TempFile GetTempFileName(string? extension = null)
        {
            var tempFile = Path.GetTempFileName();
            if (extension is not null)
            {
                var newPath = Path.ChangeExtension(tempFile, extension);
                File.Move(tempFile, newPath);
                return new DefaultTempFile(newPath, deleteParentDirectory: false);
            }
            return new DefaultTempFile(tempFile, deleteParentDirectory: false);
        }

        /// <inheritdoc/>
        public TempFile CreateTempFile(string prefix, string fileName)
        {
            var tempDir = Directory.CreateTempSubdirectory(prefix).FullName;
            var filePath = Path.Combine(tempDir, fileName);
            File.Create(filePath).Dispose();
            return new DefaultTempFile(filePath, deleteParentDirectory: true);
        }
    }

    /// <summary>
    /// Default implementation of <see cref="TempDirectory"/>.
    /// </summary>
    private sealed class DefaultTempDirectory : TempDirectory
    {
        private readonly string _path;

        public DefaultTempDirectory(string path)
        {
            _path = path;
        }

        public override string Path => _path;

        public override void Dispose()
        {
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

        public DefaultTempFile(string path, bool deleteParentDirectory)
        {
            _path = path;
            _deleteParentDirectory = deleteParentDirectory;
        }

        public override string Path => _path;

        public override void Dispose()
        {
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
