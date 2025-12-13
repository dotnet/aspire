// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics.Metrics;
using Aspire.Hosting.Execution;
using Aspire.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Tests.Execution;

/// <summary>
/// Integration tests for the real VirtualShell implementation.
/// These tests actually execute processes.
/// </summary>
public class VirtualShellIntegrationTests(ITestOutputHelper testOutputHelper)
{
    private readonly IVirtualShell _shell = CreateShell(testOutputHelper);

    private static IVirtualShell CreateShell(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddXunit(testOutputHelper);
        });
        var activitySource = new VirtualShellActivitySource();
        var meterFactory = new TestMeterFactory();
        return new VirtualShell(loggerFactory, activitySource, meterFactory)
            .WithLogging();
    }

    [Fact]
    public async Task Run_Echo_ReturnsOutput()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.RunAsync("cmd", ["/c", "echo", "hello"])
            : await _shell.RunAsync("echo", ["hello"]);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Stdout);
    }

    [Fact]
    public async Task Run_DotnetVersion_Succeeds()
    {
        var result = await _shell.RunAsync("dotnet", ["--version"]);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"\d+\.\d+", result.Stdout);
    }

    [Fact]
    public async Task Run_NonExistentCommand_Fails()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _shell.RunAsync("this-command-does-not-exist-12345"));
    }

    [Fact]
    public async Task Run_WithWorkingDirectory_UsesCorrectDirectory()
    {
        using var tempDir = new TestTempDirectory();
        var shellWithCd = _shell.Cd(tempDir.Path);

        var result = OperatingSystem.IsWindows()
            ? await shellWithCd.RunAsync("cmd", ["/c", "cd"])
            : await shellWithCd.RunAsync("pwd");

        Assert.True(result.Success);
        Assert.Contains(tempDir.Path, result.Stdout);
    }

    [Fact]
    public async Task Run_WithEnvironmentVariable_PassesVariable()
    {
        var shellWithEnv = _shell.Env("MY_TEST_VAR", "test_value_12345");

        var result = OperatingSystem.IsWindows()
            ? await shellWithEnv.RunAsync("cmd", ["/c", "echo", "%MY_TEST_VAR%"])
            : await shellWithEnv.RunAsync("sh", ["-c", "echo $MY_TEST_VAR"]);

        Assert.True(result.Success);
        Assert.Contains("test_value_12345", result.Stdout);
    }

    [Fact]
    public async Task Command_WithStdin_PassesInput()
    {
        var stdin = Stdin.FromText("hello from stdin");

        var result = OperatingSystem.IsWindows()
            ? await _shell.Command("findstr", ["."])
                .WithStdin(stdin)
                .RunAsync()
            : await _shell.Command("cat")
                .WithStdin(stdin)
                .RunAsync();

        Assert.True(result.Success);
        Assert.Contains("hello from stdin", result.Stdout);
    }

    [Fact]
    public async Task Command_WithCaptureOutputFalse_DoesNotCaptureOutput()
    {
        var result = await _shell.Command("dotnet", ["--version"])
            .WithCaptureOutput(false)
            .RunAsync();

        Assert.True(result.Success);
        Assert.True(string.IsNullOrEmpty(result.Stdout));
    }

    [Fact]
    public async Task Start_ReadLines_ReturnsAllLines()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Start("cmd", ["/c", "echo line1 & echo line2"])
            : _shell.Start("sh", ["-c", "echo line1; echo line2"]);

        var lines = new List<string>();
        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line.Text);
        }

        var result = await process.WaitAsync();
        Assert.True(result.Success);
        Assert.Contains(lines, l => l.Contains("line1"));
        Assert.Contains(lines, l => l.Contains("line2"));
    }

    [Fact]
    public async Task Start_ReadLines_StdoutHasIsStdErrFalse()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Start("cmd", ["/c", "echo hello"])
            : _shell.Start("echo", ["hello"]);

        var lines = new List<OutputLine>();
        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line);
        }

        Assert.True(await process.WaitAsync() is { Success: true });
        Assert.All(lines, l => Assert.False(l.IsStdErr));
    }

    [Fact]
    public async Task Start_ReadLines_StderrHasIsStdErrTrue()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Start("cmd", ["/c", "echo error 1>&2"])
            : _shell.Start("sh", ["-c", "echo error >&2"]);

        var lines = new List<OutputLine>();
        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line);
        }

        Assert.True(await process.WaitAsync() is { Success: true });
        Assert.Contains(lines, l => l.IsStdErr && l.Text.Contains("error"));
    }

    [Fact]
    public async Task Start_ReadLines_CanDistinguishStdoutAndStderr()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Start("cmd", ["/c", "echo stdout & echo stderr 1>&2"])
            : _shell.Start("sh", ["-c", "echo stdout; echo stderr >&2"]);

        var stdoutLines = new List<string>();
        var stderrLines = new List<string>();

        await foreach (var line in process.ReadLinesAsync())
        {
            if (line.IsStdErr)
            {
                stderrLines.Add(line.Text);
            }
            else
            {
                stdoutLines.Add(line.Text);
            }
        }

        Assert.True(await process.WaitAsync() is { Success: true });
        Assert.Contains(stdoutLines, l => l.Contains("stdout"));
        Assert.Contains(stderrLines, l => l.Contains("stderr"));
    }

    [Fact]
    public async Task Run_CommandThatExitsNonZero_ReturnsFailure()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.RunAsync("cmd", ["/c", "exit 42"])
            : await _shell.RunAsync("sh", ["-c", "exit 42"]);

        Assert.False(result.Success);
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public async Task Run_CommandWithStderr_CapturesStderr()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.RunAsync("cmd", ["/c", "echo error message 1>&2"])
            : await _shell.RunAsync("sh", ["-c", "echo 'error message' >&2"]);

        Assert.True(result.Success);
        Assert.Contains("error", result.Stderr?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public async Task Run_WithCancellation_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _shell.RunAsync("dotnet", ["--version"], cts.Token));
    }

    [Fact]
    public async Task Shell_IsImmutable_StateChangesCreateNewInstances()
    {
        using var tempDir = new TestTempDirectory();

        var shell1 = _shell;
        var shell2 = shell1.Cd(tempDir.Path);
        var shell3 = shell2.Env("VAR", "value");

        var result1 = await shell1.RunAsync("dotnet", ["--version"]);
        var result2 = await shell2.RunAsync("dotnet", ["--version"]);
        var result3 = await shell3.RunAsync("dotnet", ["--version"]);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.True(result3.Success);
    }

    [Fact]
    public async Task ProcessResult_EnsureSuccess_DoesNotThrowOnSuccess()
    {
        var result = await _shell.RunAsync("dotnet", ["--version"]);

        result.EnsureSuccess();
    }

    [Fact]
    public async Task ProcessResult_EnsureSuccess_ThrowsOnFailure()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.RunAsync("cmd", ["/c", "exit 1"])
            : await _shell.RunAsync("sh", ["-c", "exit 1"]);

        Assert.Throws<InvalidOperationException>(() => result.EnsureSuccess());
    }

    [Fact]
    [RequiresUnixSystem("SIGTERM is not supported on Windows")]
    public async Task Start_Signal_SendsTermToProcess()
    {
        // Start a process that:
        // 1. Sets up a SIGTERM trap that prints confirmation
        // 2. Prints "READY" so we know the trap is set up
        // 3. Loops with short sleeps so the trap handler runs promptly
        await using var process = _shell.Start("sh", ["-c",
            "trap 'echo SIGTERM_RECEIVED; exit 0' TERM; echo READY; while true; do sleep 0.1; done"]);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var isReady = false;
        var receivedSignal = false;

        try
        {
            await foreach (var line in process.ReadLinesAsync().WithCancellation(cts.Token))
            {
                if (!isReady && line.Text.Contains("READY"))
                {
                    isReady = true;
                    // Now that trap is set up, send the termination signal
                    process.Signal(ProcessSignal.Terminate);
                    continue;
                }

                if (line.Text.Contains("SIGTERM_RECEIVED"))
                {
                    receivedSignal = true;
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Assert.Fail($"Test timed out. isReady={isReady}, receivedSignal={receivedSignal}. " +
                "The process did not respond to the signal in time.");
        }

        var result = await process.WaitAsync(cts.Token);

        Assert.True(isReady, "Process did not output READY - trap may not have been set up");
        Assert.True(receivedSignal, "Process did not output SIGTERM_RECEIVED - signal may not have been delivered");
        Assert.Equal(0, result.ExitCode);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new Meter(options);
        public void Dispose() { }
    }
}
