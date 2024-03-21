// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Dcp;

using LogEntry = (string Content, bool IsErrorMessage);
using LogEntryList = IReadOnlyList<(string Content, bool IsErrorMessage)>;

internal sealed class ResourceLogSource<TResource>(
    ILogger logger,
    IKubernetesService kubernetesService,
    TResource resource) :
    IAsyncEnumerable<LogEntryList>
    where TResource : CustomResource
{
    public async IAsyncEnumerator<LogEntryList> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            throw new ArgumentException("Cancellation token must be cancellable in order to prevent leaking resources.", nameof(cancellationToken));
        }

        var timestamps = resource is Container; // Timestamps are available only for Containers as of Aspire P5.
        var stdoutStream = await kubernetesService.GetLogStreamAsync(resource, Logs.StreamTypeStdOut, follow: true, timestamps: timestamps, cancellationToken).ConfigureAwait(false);
        var stderrStream = await kubernetesService.GetLogStreamAsync(resource, Logs.StreamTypeStdErr, follow: true, timestamps: timestamps, cancellationToken).ConfigureAwait(false);

        var channel = Channel.CreateUnbounded<LogEntry>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });

        var stdoutStreamTask = Task.Run(() => StreamLogsAsync(stdoutStream, isError: false), cancellationToken);
        var stderrStreamTask = Task.Run(() => StreamLogsAsync(stderrStream, isError: true), cancellationToken);

        // End the enumeration when both streams have been read to completion.
        _ = Task.WhenAll(stdoutStreamTask, stderrStreamTask).ContinueWith
            (_ => { channel.Writer.TryComplete(); },
            cancellationToken,
            TaskContinuationOptions.None,
            TaskScheduler.Default).ConfigureAwait(false);

        await foreach (var batch in channel.GetBatchesAsync(cancellationToken: cancellationToken).ConfigureAwait(true)) // Setting ConfigureAwait to silence analyzer. Consider calling ConfigureAwait(false)
        {
            yield return batch;
        }

        async Task StreamLogsAsync(Stream stream, bool isError)
        {
            try
            {
                using var sr = new StreamReader(stream, leaveOpen: false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is null)
                    {
                        return; // No more data
                    }

                    var succeeded = channel.Writer.TryWrite((line, isError));
                    if (!succeeded)
                    {
                        logger.LogWarning("Failed to write log entry to channel. Logs for {Kind} {Name} may be incomplete", resource.Kind, resource.Metadata.Name);
                        channel.Writer.TryComplete();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error happened when capturing logs for {Kind} {Name}", resource.Kind, resource.Metadata.Name);
                channel.Writer.TryComplete(ex);
            }
        }
    }
}
