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
            return new TempDirectory(path);
        }

        /// <inheritdoc/>
        public TempFile GetTempFileName(string? extension = null)
        {
            var tempFile = Path.GetTempFileName();
            if (extension is not null)
            {
                var newPath = Path.ChangeExtension(tempFile, extension);
                File.Move(tempFile, newPath);
                return new TempFile(newPath);
            }
            return new TempFile(tempFile);
        }

        /// <inheritdoc/>
        public TempFile CreateTempFile(string prefix, string fileName)
        {
            var tempDir = Directory.CreateTempSubdirectory(prefix).FullName;
            var filePath = Path.Combine(tempDir, fileName);
            File.Create(filePath).Dispose();
            return new TempFile(filePath, deleteParentDirectory: true);
        }
    }
}
