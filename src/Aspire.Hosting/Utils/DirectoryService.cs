// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Default implementation of <see cref="IDirectoryService"/>.
/// </summary>
internal sealed class DirectoryService : IDirectoryService
{
    private readonly TempDirectoryService _tempDirectory = new();

    /// <inheritdoc/>
    public ITempDirectoryService TempDirectory => _tempDirectory;

    /// <summary>
    /// Implementation of <see cref="ITempDirectoryService"/>.
    /// </summary>
    private sealed class TempDirectoryService : ITempDirectoryService
    {
        /// <inheritdoc/>
        public string CreateTempSubdirectory(string? prefix = null)
        {
            return Directory.CreateTempSubdirectory(prefix ?? "aspire").FullName;
        }
    }
}
