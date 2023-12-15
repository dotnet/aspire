// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Aspire.Hosting.Extensions;

namespace Aspire.Hosting.Dashboard;

internal sealed partial class FileLogSource(string stdOutPath, string stdErrPath) : IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>
{
    private static readonly StreamPipeReaderOptions s_streamPipeReaderOptions = new(leaveOpen: true);
    private static readonly Regex s_lineSplitRegex = GenerateLineSplitRegex();

    public async IAsyncEnumerator<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<(string Content, bool IsErrorMessage)>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });

        var stdOut = Task.Run(() => WatchFileAsync(stdOutPath, isError: false), cancellationToken);
        var stdErr = Task.Run(() => WatchFileAsync(stdErrPath, isError: true), cancellationToken);

        await foreach (var batch in channel.GetBatches(cancellationToken))
        {
            yield return batch;
        }

        async Task WatchFileAsync(string filePath, bool isError)
        {
            var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            // Close the file stream when the cancellation token fires.
            // It's important that callers cancel when no longer needed.
            using var _ = fileStream;

            var partialLine = "";

            // The FileStream will stay open and continue growing as data is written to it
            // but the PipeReader will close as soon as it reaches the end of the FileStream.
            // So we need to keep re-creating it. It will read from the last position 
            while (!cancellationToken.IsCancellationRequested)
            {
                var reader = PipeReader.Create(fileStream, s_streamPipeReaderOptions);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

                    if (result.IsCompleted)
                    {
                        // There's no more data in the file. Because we are polling, we will loop
                        // around again and land back here almost immediately. We introduce a small
                        // sleep here in order to not burn CPU while polling. This sleep won't limit
                        // the rate at which we can consume file changes when many exist, as the sleep
                        // only occurs when we have caught up.
                        //
                        // Longer term we hope to have a log streaming API from DCP for this.
                        // https://github.com/dotnet/aspire/issues/760
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                        // We're done here. Loop around and wait for a signal that the file has changed.
                        break;
                    }

                    var str = Encoding.UTF8.GetString(result.Buffer);

                    // It's possible that we don't read an entire log line at the end of the data we're reading.
                    // If that's the case, we'll wait for the next iteration, grab the rest and concatenate them.
                    var lines = s_lineSplitRegex.Split(str);
                    var isLastLineComplete = str[^1] is '\r' or '\n' && lines[^1] is not { Length: 0 };
                    lines[0] = partialLine + lines[0];
                    partialLine = isLastLineComplete ? "" : lines[^1];

                    reader.AdvanceTo(GetEndPosition(result.Buffer));

                    var count = isLastLineComplete ? lines.Length : lines.Length - 1;

                    for (var i = 0; i < count; i++)
                    {
                        channel.Writer.TryWrite((lines[i], isError));
                    }
                }

                reader.Complete();
            }

            static SequencePosition GetEndPosition(in ReadOnlySequence<byte> buffer)
            {
                var sequenceReader = new SequenceReader<byte>(buffer);
                sequenceReader.AdvanceToEnd();
                return sequenceReader.Position;
            }
        }
    }

    [GeneratedRegex("""\r\n|\r|\n""", RegexOptions.CultureInvariant)]
    private static partial Regex GenerateLineSplitRegex();
}
