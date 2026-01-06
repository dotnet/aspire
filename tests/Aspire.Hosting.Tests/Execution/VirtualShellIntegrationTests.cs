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
            ? await _shell.Command("cmd", ["/c", "echo", "hello"]).RunAsync()
            : await _shell.Command("echo", ["hello"]).RunAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Stdout);
    }

    [Fact]
    public async Task Run_DotnetVersion_Succeeds()
    {
        var result = await _shell.Command("dotnet", ["--version"]).RunAsync();

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Matches(@"\d+\.\d+", result.Stdout);
    }

    [Fact]
    public async Task Run_NonExistentCommand_Fails()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _shell.Command("this-command-does-not-exist-12345").RunAsync());
    }

    [Fact]
    public async Task Run_WithWorkingDirectory_UsesCorrectDirectory()
    {
        using var tempDir = new TestTempDirectory();
        var shellWithCd = _shell.Cd(tempDir.Path);

        var result = OperatingSystem.IsWindows()
            ? await shellWithCd.Command("cmd", ["/c", "cd"]).RunAsync()
            : await shellWithCd.Command("pwd").RunAsync();

        Assert.True(result.Success);
        Assert.Contains(tempDir.Path, result.Stdout);
    }

    [Fact]
    public async Task Run_WithEnvironmentVariable_PassesVariable()
    {
        var shellWithEnv = _shell.Env("MY_TEST_VAR", "test_value_12345");

        var result = OperatingSystem.IsWindows()
            ? await shellWithEnv.Command("cmd", ["/c", "echo", "%MY_TEST_VAR%"]).RunAsync()
            : await shellWithEnv.Command("sh", ["-c", "echo $MY_TEST_VAR"]).RunAsync();

        Assert.True(result.Success);
        Assert.Contains("test_value_12345", result.Stdout);
    }

    [Fact]
    public async Task RunAsync_WithStdin_PassesInput()
    {
        var stdin = ProcessInput.FromText("hello from stdin");

        var result = OperatingSystem.IsWindows()
            ? await _shell.Command("findstr", ["."]).RunAsync(stdin: stdin)
            : await _shell.Command("cat").RunAsync(stdin: stdin);

        Assert.True(result.Success);
        Assert.Contains("hello from stdin", result.Stdout);
    }

    [Fact]
    public async Task RunAsync_WithCaptureFalse_DoesNotCaptureOutput()
    {
        var result = await _shell.Command("dotnet", ["--version"]).RunAsync(capture: false);

        Assert.True(result.Success);
        Assert.True(string.IsNullOrEmpty(result.Stdout));
    }

    [Fact]
    public async Task StartReading_ReadLines_ReturnsAllLines()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Command("cmd", ["/c", "echo line1 & echo line2"]).StartReading()
            : _shell.Command("sh", ["-c", "echo line1; echo line2"]).StartReading();

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
    public async Task StartReading_ReadLines_StdoutHasIsStdErrFalse()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Command("cmd", ["/c", "echo hello"]).StartReading()
            : _shell.Command("echo", ["hello"]).StartReading();

        var lines = new List<OutputLine>();
        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line);
        }

        Assert.True(await process.WaitAsync() is { Success: true });
        Assert.All(lines, l => Assert.False(l.IsStdErr));
    }

    [Fact]
    public async Task StartReading_ReadLines_StderrHasIsStdErrTrue()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Command("cmd", ["/c", "echo error 1>&2"]).StartReading()
            : _shell.Command("sh", ["-c", "echo error >&2"]).StartReading();

        var lines = new List<OutputLine>();
        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line);
        }

        Assert.True(await process.WaitAsync() is { Success: true });
        Assert.Contains(lines, l => l.IsStdErr && l.Text.Contains("error"));
    }

    [Fact]
    public async Task StartReading_ReadLines_CanDistinguishStdoutAndStderr()
    {
        await using var process = OperatingSystem.IsWindows()
            ? _shell.Command("cmd", ["/c", "echo stdout & echo stderr 1>&2"]).StartReading()
            : _shell.Command("sh", ["-c", "echo stdout; echo stderr >&2"]).StartReading();

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
            ? await _shell.Command("cmd", ["/c", "exit 42"]).RunAsync()
            : await _shell.Command("sh", ["-c", "exit 42"]).RunAsync();

        Assert.False(result.Success);
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public async Task Run_CommandWithStderr_CapturesStderr()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.Command("cmd", ["/c", "echo error message 1>&2"]).RunAsync()
            : await _shell.Command("sh", ["-c", "echo 'error message' >&2"]).RunAsync();

        Assert.True(result.Success);
        Assert.Contains("error", result.Stderr?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public async Task Run_WithCancellation_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _shell.Command("dotnet", ["--version"]).RunAsync(ct: cts.Token));
    }

    [Fact]
    public async Task Shell_IsImmutable_StateChangesCreateNewInstances()
    {
        using var tempDir = new TestTempDirectory();

        var shell1 = _shell;
        var shell2 = shell1.Cd(tempDir.Path);
        var shell3 = shell2.Env("VAR", "value");

        var result1 = await shell1.Command("dotnet", ["--version"]).RunAsync();
        var result2 = await shell2.Command("dotnet", ["--version"]).RunAsync();
        var result3 = await shell3.Command("dotnet", ["--version"]).RunAsync();

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.True(result3.Success);
    }

    [Fact]
    public async Task ProcessResult_EnsureSuccess_DoesNotThrowOnSuccess()
    {
        var result = await _shell.Command("dotnet", ["--version"]).RunAsync();

        result.EnsureSuccess();
    }

    [Fact]
    public async Task ProcessResult_EnsureSuccess_ThrowsOnFailure()
    {
        var result = OperatingSystem.IsWindows()
            ? await _shell.Command("cmd", ["/c", "exit 1"]).RunAsync()
            : await _shell.Command("sh", ["-c", "exit 1"]).RunAsync();

        Assert.Throws<InvalidOperationException>(() => result.EnsureSuccess());
    }

    [Fact]
    [RequiresUnixSystem("SIGTERM is not supported on Windows")]
    public async Task StartReading_Signal_SendsTermToProcess()
    {
        // Start a process that:
        // 1. Sets up a SIGTERM trap that prints confirmation
        // 2. Prints "READY" so we know the trap is set up
        // 3. Loops with short sleeps so the trap handler runs promptly
        await using var process = _shell.Command("sh", ["-c",
            "trap 'echo SIGTERM_RECEIVED; exit 0' TERM; echo READY; while true; do sleep 0.1; done"]).StartReading();

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
