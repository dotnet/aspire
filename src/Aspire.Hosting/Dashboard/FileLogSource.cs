// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using Aspire.Dashboard.Model;

namespace Aspire.Hosting.Dashboard;

internal sealed class FileLogSource(string? stdOutPath, string? stdErrPath) : ILogSource
{
    private readonly string? _stdOutPath = stdOutPath;
    private readonly string? _stdErrPath = stdErrPath;

    public ValueTask<bool> StartAsync(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_stdOutPath is not null || _stdErrPath is not null);
    }

    public async IAsyncEnumerable<string[]> WatchOutputLogAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        if (_stdOutPath is not null)
        {
            await foreach (var logs in WatchLogAsync(_stdOutPath, cancellationToken))
            {
                yield return logs;
            }
        }
    }

    public async IAsyncEnumerable<string[]> WatchErrorLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_stdErrPath is not null)
        {
            await foreach (var logs in WatchLogAsync(_stdErrPath, cancellationToken))
            {
                yield return logs;
            }
        }
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    private static readonly StreamPipeReaderOptions s_streamPipeReaderOptions = new(leaveOpen: true);
    private static readonly string[] s_lineSeparators = ["\r", "\n", "\r\n"];
    private static bool IsNewLine(char c) => c == '\r' || c == '\n';

    private static async IAsyncEnumerable<string[]> WatchLogAsync(string filePath, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        var partialLine = string.Empty;

        // The FileStream will stay open and continue growing as data is written to it
        // but the PipeReader will close as soon as it reaches the end of the FileStream.
        // So we need to keep re-creating it. It will read from the last position 
        while (!cancellationToken.IsCancellationRequested)
        {
            var reader = PipeReader.Create(fileStream, s_streamPipeReaderOptions);
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await reader!.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (result.IsCompleted)
                {
                    break;
                }

                var logs = Encoding.UTF8.GetString(result.Buffer);

                // It's possible that we don't read an entire log line at the end of the data we're reading.
                // If that's the case, we'll wait for the next iteration, grab the rest and concatenate them.
                var lastLineComplete = IsNewLine(logs[^1]);
                var lines = logs.Split(s_lineSeparators, StringSplitOptions.RemoveEmptyEntries);
                lines[0] = partialLine + lines[0];
                partialLine = lastLineComplete ? string.Empty : lines[^1];

                var numberOfLinesToSend = lastLineComplete ? lines.Length : lines.Length - 1;

                yield return lines[..numberOfLinesToSend]; // end of range is exclusive

                var position = GetPosition(result.Buffer);

                reader.AdvanceTo(position);
            }

            reader.Complete();
        }

        static SequencePosition GetPosition(in ReadOnlySequence<byte> buffer)
        {
            var sequenceReader = new SequenceReader<byte>(buffer);
            sequenceReader.AdvanceToEnd();
            return sequenceReader.Position;
        }
    }
}
