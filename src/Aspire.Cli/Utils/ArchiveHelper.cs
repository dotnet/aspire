// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Tar;
using System.IO.Compression;

namespace Aspire.Cli.Utils;

/// <summary>
/// Shared utilities for extracting archive files (.zip and .tar.gz).
/// </summary>
internal static class ArchiveHelper
{
    /// <summary>
    /// Extracts an archive to the specified directory, supporting .zip and .tar.gz formats.
    /// </summary>
    internal static async Task ExtractAsync(string archivePath, string destinationPath, CancellationToken cancellationToken)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles: true);
        }
        else if (archivePath.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase))
        {
            await using var fileStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            await TarFile.ExtractToDirectoryAsync(gzipStream, destinationPath, overwriteFiles: true, cancellationToken);
        }
        else
        {
            throw new NotSupportedException($"Unsupported archive format: {archivePath}");
        }
    }
}
