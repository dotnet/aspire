// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that uses
/// <see cref="System.Diagnostics.Process"/> for direct process execution.
/// </summary>
internal sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin,
        bool capture,
        CancellationToken ct)
    {
        var stdout = capture ? ProcessOutput.Capture : ProcessOutput.Null;
        var stderr = capture ? ProcessOutput.Capture : ProcessOutput.Null;

        var process = Start(exePath, args, state, stdin, stdout, stderr);
        await using (process.ConfigureAwait(false))
        {
            return await process.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ProcessLines StartReading(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin)
    {
        var systemProcess = CreateAndStartProcess(exePath, args, state);
        var processLines = new ProcessLines(systemProcess);

        // Handle stdin asynchronously if provided
        if (stdin is not null)
        {
            _ = WriteStdinAsync(processLines, stdin, CancellationToken.None);
        }

        return processLines;
    }

    /// <inheritdoc />
    public ProcessPipes StartProcess(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state)
    {
        var process = CreateAndStartProcess(exePath, args, state);
        return new ProcessPipes(process);
    }

    /// <inheritdoc />
    public ProcessHandle Start(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin,
        ProcessOutput stdout,
        ProcessOutput stderr)
    {
        var systemProcess = CreateAndStartProcess(exePath, args, state);
        var processHandle = new ProcessHandle(systemProcess, stdout, stderr);

        // Handle stdin asynchronously if provided
        if (stdin is not null)
        {
            _ = WriteStdinAsync(processHandle, stdin, CancellationToken.None);
        }

        return processHandle;
    }

    private static System.Diagnostics.Process CreateAndStartProcess(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = state.WorkingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
#if NET10_0_OR_GREATER
            // On Windows, create a new process group so we can send CTRL+C via GenerateConsoleCtrlEvent
            CreateNewProcessGroup = OperatingSystem.IsWindows()
#endif
        };

        // Build arguments
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        // Setup environment from shell state
        foreach (var (key, value) in state.Environment)
        {
            if (value is null)
            {
                startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        var process = new System.Diagnostics.Process { StartInfo = startInfo };
        process.Start();

        return process;
    }

    private static async Task WriteStdinAsync(ProcessLines processLines, ProcessInput stdin, CancellationToken ct)
    {
        try
        {
            await stdin.WriteAsync(processLines.Input, ct).ConfigureAwait(false);
        }
        finally
        {
            if (stdin.AutoComplete)
            {
                await processLines.Input.CompleteAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task WriteStdinAsync(ProcessHandle processHandle, ProcessInput stdin, CancellationToken ct)
    {
        try
        {
            await stdin.WriteAsync(processHandle.Input, ct).ConfigureAwait(false);
        }
        finally
        {
            if (stdin.AutoComplete)
            {
                await processHandle.Input.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
