// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.VirtualShell.Internal;

/// <summary>
/// Default implementation of <see cref="IProcessRunner"/> that uses
/// <see cref="System.Diagnostics.Process"/> for direct process execution.
/// </summary>
internal sealed class ProcessRunner : IProcessRunner
{
    /// <inheritdoc />
    public async Task<CliResult> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExecSpec spec,
        ShellState state,
        CancellationToken ct)
    {
        var streamRun = Start(exePath, args, spec, state);
        await using (streamRun.ConfigureAwait(false))
        {
            // Handle timeout
            if (spec.Timeout.HasValue)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(spec.Timeout.Value);

                try
                {
                    return await streamRun.ResultAsync(timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Timeout occurred
                    streamRun.Kill();
                    var result = await streamRun.ResultAsync(CancellationToken.None).ConfigureAwait(false);
                    return result with { Reason = CliExitReason.TimedOut };
                }
            }

            return await streamRun.ResultAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public StreamRun Start(
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
            RedirectStandardInput = spec.Stdin != null,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
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

        var streamRun = new StreamRun(
            process,
            spec,
            captureOutput: spec.CaptureOutput,
            killProcessTree: spec.KillProcessTree);

        // Handle stdin asynchronously
        if (spec.Stdin != null)
        {
            _ = WriteStdinAsync(process, spec.Stdin, CancellationToken.None);
        }

        return streamRun;
    }

    private static async Task WriteStdinAsync(Process process, Stdin stdin, CancellationToken ct)
    {
        try
        {
            switch (stdin)
            {
                case Stdin.TextStdin textStdin:
                    await process.StandardInput.WriteAsync(textStdin.Text.AsMemory(), ct).ConfigureAwait(false);
                    break;

                case Stdin.BytesStdin bytesStdin:
                    await process.StandardInput.BaseStream.WriteAsync(bytesStdin.Bytes, ct).ConfigureAwait(false);
                    break;

                case Stdin.StreamStdin streamStdin:
                    try
                    {
                        await streamStdin.Stream.CopyToAsync(process.StandardInput.BaseStream, ct).ConfigureAwait(false);
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
                        await fileStream.CopyToAsync(process.StandardInput.BaseStream, ct).ConfigureAwait(false);
                    }
                    break;

                case Stdin.WriterStdin writerStdin:
                    await writerStdin.WriteAsync(process.StandardInput.BaseStream, ct).ConfigureAwait(false);
                    break;
            }
        }
        finally
        {
            await process.StandardInput.FlushAsync(ct).ConfigureAwait(false);
            process.StandardInput.Close();
        }
    }
}
