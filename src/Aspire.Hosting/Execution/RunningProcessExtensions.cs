// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Extension methods for <see cref="IRunningProcess"/>.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public static class RunningProcessExtensions
{
    /// <summary>
    /// Reads lines from both stdout and stderr, yielding them as they become available.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    public static async IAsyncEnumerable<OutputLine> ReadLinesAsync(
        this IRunningProcess process,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var channel = Channel.CreateUnbounded<OutputLine>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        // Start reading from both stdout and stderr concurrently
        var stdoutTask = ReadLinesFromReaderAsync(process.Output, isStdErr: false, channel.Writer, ct);
        var stderrTask = ReadLinesFromReaderAsync(process.Error, isStdErr: true, channel.Writer, ct);

        // Complete the channel when both readers are done
        _ = Task.WhenAll(stdoutTask, stderrTask).ContinueWith(
            _ => channel.Writer.TryComplete(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        await foreach (var line in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    private static async Task ReadLinesFromReaderAsync(
        PipeReader reader,
        bool isStdErr,
        ChannelWriter<OutputLine> writer,
        CancellationToken ct)
    {
        try
        {
            while (true)
            {
                ReadResult result;
                try
                {
                    result = await reader.ReadAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                var buffer = result.Buffer;

                while (TryReadLine(ref buffer, out var line))
                {
                    await writer.WriteAsync(new OutputLine(isStdErr, line), ct).ConfigureAwait(false);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    // Process any remaining content as a final line
                    if (buffer.Length > 0)
                    {
                        var remainingLine = GetString(buffer);
                        if (!string.IsNullOrEmpty(remainingLine))
                        {
                            await writer.WriteAsync(new OutputLine(isStdErr, remainingLine), ct).ConfigureAwait(false);
                        }
                    }
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out string line)
    {
        var reader = new SequenceReader<byte>(buffer);

        if (reader.TryReadTo(out ReadOnlySpan<byte> lineBytes, (byte)'\n'))
        {
            // Remove trailing \r if present
            if (lineBytes.Length > 0 && lineBytes[^1] == '\r')
            {
                lineBytes = lineBytes[..^1];
            }

            line = Encoding.UTF8.GetString(lineBytes);
            buffer = buffer.Slice(reader.Position);
            return true;
        }

        line = string.Empty;
        return false;
    }

    private static string GetString(ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsSingleSegment)
        {
            var span = buffer.FirstSpan;
            // Remove trailing \r if present
            if (span.Length > 0 && span[^1] == '\r')
            {
                span = span[..^1];
            }
            return Encoding.UTF8.GetString(span);
        }

        return Encoding.UTF8.GetString(buffer.ToArray()).TrimEnd('\r');
    }

    /// <summary>
    /// Writes bytes to the process's standard input.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="data">The bytes to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteAsync(
        this IRunningProcess process,
        ReadOnlyMemory<byte> data,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        await process.Input.WriteAsync(data, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes text to the process's standard input using UTF-8 encoding.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="text">The text to write.</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteAsync(
        this IRunningProcess process,
        ReadOnlyMemory<char> text,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var byteCount = Encoding.UTF8.GetByteCount(text.Span);
        var bytes = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var actualBytes = Encoding.UTF8.GetBytes(text.Span, bytes);
            await process.Input.WriteAsync(bytes.AsMemory(0, actualBytes), ct).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    /// <summary>
    /// Writes a line of text to the process's standard input using UTF-8 encoding.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="line">The line to write (newline will be appended).</param>
    /// <param name="ct">A cancellation token.</param>
    public static async Task WriteLineAsync(
        this IRunningProcess process,
        string line,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);
        ArgumentNullException.ThrowIfNull(line);

        var lineWithNewline = line + Environment.NewLine;
        var bytes = Encoding.UTF8.GetBytes(lineWithNewline);
        await process.Input.WriteAsync(bytes, ct).ConfigureAwait(false);
    }
    /// <summary>
    /// Ensures the process completed successfully, throwing if it did not.
    /// </summary>
    /// <param name="process">The running process.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process did not complete successfully.
    /// </exception>
    public static async Task EnsureSuccessAsync(
        this IRunningProcess process,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(process);

        var result = await process.WaitAsync(ct).ConfigureAwait(false);
        if (!result.Success)
        {
            var message = $"Process exited with code {result.ExitCode} (reason: {result.Reason})";
            if (!string.IsNullOrWhiteSpace(result.Stderr))
            {
                message += $": {result.Stderr}";
            }
            throw new InvalidOperationException(message);
        }
    }
}
