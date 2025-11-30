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
        public string CreateTempSubdirectory(string? prefix = null)
        {
            return Directory.CreateTempSubdirectory(prefix ?? "aspire").FullName;
        }

        /// <inheritdoc/>
        public string GetTempFileName(string? extension = null)
        {
            var tempFile = Path.GetTempFileName();
            if (extension is not null)
            {
                var newPath = Path.ChangeExtension(tempFile, extension);
                File.Move(tempFile, newPath);
                return newPath;
            }
            return tempFile;
        }

        /// <inheritdoc/>
        public string CreateTempFile(string prefix, string fileName)
        {
            var tempDir = CreateTempSubdirectory(prefix);
            var filePath = Path.Combine(tempDir, fileName);
            File.Create(filePath).Dispose();
            return filePath;
        }
    }
}
