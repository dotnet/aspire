// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;

namespace Aspire.Cli.Tests.TestServices;

/// <summary>
/// Test implementation of ICliDownloader for unit tests.
/// </summary>
internal sealed class TestCliDownloader : ICliDownloader
{
    private readonly DirectoryInfo _tempDirectory;

    public TestCliDownloader(DirectoryInfo tempDirectory)
    {
        _tempDirectory = tempDirectory;
    }

    public Func<string, CancellationToken, Task<string>>? DownloadLatestCliAsyncCallback { get; set; }

    public Task<string> DownloadLatestCliAsync(string quality, CancellationToken cancellationToken)
    {
        if (DownloadLatestCliAsyncCallback is not null)
        {
            return DownloadLatestCliAsyncCallback(quality, cancellationToken);
        }

        // Ensure the directory exists
        if (!_tempDirectory.Exists)
        {
            _tempDirectory.Create();
        }

        // Generate a unique filename for the test download
        var filename = $"test-cli-download-{Guid.NewGuid():N}";
        var path = Path.Combine(_tempDirectory.FullName, filename);
        
        return Task.FromResult(path);
    }
}
