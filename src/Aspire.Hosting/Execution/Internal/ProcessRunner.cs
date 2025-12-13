// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;
using System.IO.Pipelines;

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
        ExecSpec spec,
        ShellState state,
        CancellationToken ct)
    {
        var streamRun = Start(exePath, args, spec, state);
        await using (streamRun.ConfigureAwait(false))
        {
            return await streamRun.WaitAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public RunningProcess Start(
        string exePath,
        IReadOnlyList<string> args,
        ExecSpec spec,
        ShellState state)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = spec.WorkingDirectory ?? state.WorkingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // Always redirect stdin for symmetry with output
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

        // Setup environment
        // First, apply shell state environment
        foreach (var (key, value) in state.Environment)
        {
            if (value == null)
            {
                startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        // Then, apply per-call environment overrides
        foreach (var (key, value) in spec.Environment)
        {
            if (value == null)
            {
                startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var runningProcess = new RunningProcess(
            process,
            spec,
            captureOutput: spec.CaptureOutput);

        // Handle stdin asynchronously by writing to the Input pipe
        if (spec.Stdin != null)
        {
            _ = WriteStdinAsync(runningProcess.Input, spec.Stdin, CancellationToken.None);
        }

        return runningProcess;
    }

    private static async Task WriteStdinAsync(PipeWriter writer, Stdin stdin, CancellationToken ct)
    {
        // IsPipe means the user will write to Input manually - don't write or complete
        if (stdin.IsPipe)
        {
            return;
        }

        try
        {
            switch (stdin)
            {
                case Stdin.TextStdin textStdin:
                    var bytes = textStdin.Encoding.GetBytes(textStdin.Text);
                    await writer.WriteAsync(bytes, ct).ConfigureAwait(false);
                    break;

                case Stdin.BytesStdin bytesStdin:
                    await writer.WriteAsync(bytesStdin.Bytes, ct).ConfigureAwait(false);
                    break;

                case Stdin.StreamStdin streamStdin:
                    try
                    {
                        await streamStdin.Stream.CopyToAsync(writer, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        if (!streamStdin.LeaveOpen)
                        {
                            await streamStdin.Stream.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    break;

                case Stdin.FileStdin fileStdin:
                    var fileStream = File.OpenRead(fileStdin.Path);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        await fileStream.CopyToAsync(writer, ct).ConfigureAwait(false);
                    }
                    break;

                case Stdin.WriterStdin writerStdin:
                    // For writer stdin, create a stream adapter over the pipe
                    var pipeStream = writer.AsStream(leaveOpen: true);
                    await writerStdin.WriteAsync(pipeStream, ct).ConfigureAwait(false);
                    await pipeStream.FlushAsync(ct).ConfigureAwait(false);
                    break;
            }
        }
        finally
        {
            // Complete the pipe to signal end of input
            await writer.CompleteAsync().ConfigureAwait(false);
        }
    }
}
