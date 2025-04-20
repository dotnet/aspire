// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit;

namespace Aspire.Cli.Tests.Utils;

internal sealed class TemporaryRepo(ITestOutputHelper outputHelper, DirectoryInfo repoDirectory) : IDisposable
{
    public DirectoryInfo RootDirectory => repoDirectory;

    public DirectoryInfo CreateDirectory(string name)
    {
        return repoDirectory.CreateSubdirectory(name);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        outputHelper.WriteLine($"Initializing git repository at: {repoDirectory.FullName}");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "init",
                WorkingDirectory = repoDirectory.FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to initialize git repository: {error}");
        }
    }

    public void Dispose()
    {
        try
        {
            repoDirectory.Delete(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing TemporaryRepo: {ex.Message}");
        }
    }

    internal static TemporaryRepo Create(ITestOutputHelper outputHelper)
    {
        var tempPath = Path.GetTempPath();
        var path = Path.Combine(tempPath, "Aspire.Cli.Tests", "TemporaryRepo", Guid.NewGuid().ToString());
        var repoDirectory = Directory.CreateDirectory(path);
        outputHelper.WriteLine($"Temporary repo created at: {repoDirectory.FullName}");

        return new TemporaryRepo(outputHelper, repoDirectory);
    }
}
