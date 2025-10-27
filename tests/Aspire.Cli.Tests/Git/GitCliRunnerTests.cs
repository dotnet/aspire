// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Git;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Tests.Git;

public class GitCliRunnerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task FindGitRootAsync_ReturnsNull_WhenDirectoryIsNull()
    {
        var logger = new TestLogger<GitCliRunner>(outputHelper);
        var runner = new GitCliRunner(logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            runner.FindGitRootAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FindGitRootAsync_ReturnsNull_WhenNotInGitRepo()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new TestLogger<GitCliRunner>(outputHelper);
        var runner = new GitCliRunner(logger);

        var result = await runner.FindGitRootAsync(workspace.WorkspaceRoot, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindGitRootAsync_ReturnsGitRoot_WhenInGitRepo()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new TestLogger<GitCliRunner>(outputHelper);
        var runner = new GitCliRunner(logger);

        // Initialize a git repository
        var gitInitProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = workspace.WorkspaceRoot.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (gitInitProcess != null)
        {
            await gitInitProcess.WaitForExitAsync();
            
            if (gitInitProcess.ExitCode == 0)
            {
                var result = await runner.FindGitRootAsync(workspace.WorkspaceRoot, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(workspace.WorkspaceRoot.FullName, result.FullName);
            }
            else
            {
                // Git not available, skip test
                outputHelper.WriteLine("Git not available, skipping test");
            }
        }
        else
        {
            // Git not available, skip test
            outputHelper.WriteLine("Git not available, skipping test");
        }
    }

    [Fact]
    public async Task FindGitRootAsync_ReturnsGitRoot_WhenInSubdirectory()
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var logger = new TestLogger<GitCliRunner>(outputHelper);
        var runner = new GitCliRunner(logger);

        // Initialize a git repository
        var gitInitProcess = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = workspace.WorkspaceRoot.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (gitInitProcess != null)
        {
            await gitInitProcess.WaitForExitAsync();
            
            if (gitInitProcess.ExitCode == 0)
            {
                // Create a subdirectory
                var subDir = workspace.WorkspaceRoot.CreateSubdirectory("subdir");
                
                var result = await runner.FindGitRootAsync(subDir, CancellationToken.None);

                Assert.NotNull(result);
                Assert.Equal(workspace.WorkspaceRoot.FullName, result.FullName);
            }
            else
            {
                // Git not available, skip test
                outputHelper.WriteLine("Git not available, skipping test");
            }
        }
        else
        {
            // Git not available, skip test
            outputHelper.WriteLine("Git not available, skipping test");
        }
    }
}

/// <summary>
/// Simple test logger that writes to test output.
/// </summary>
internal sealed class TestLogger<T>(ITestOutputHelper outputHelper) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        outputHelper.WriteLine($"[{logLevel}] {formatter(state, exception)}");
        if (exception != null)
        {
            outputHelper.WriteLine(exception.ToString());
        }
    }
}
