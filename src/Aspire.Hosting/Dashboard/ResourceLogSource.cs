// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Dashboard;

internal sealed class ResourceLogSource<R>(IKubernetesService kubernetesService, R resource):
    IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>
    where R: CustomResource
{
    public async IAsyncEnumerator<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            throw new ArgumentException("Cancellation token must be cancellable in order to prevent leaking resources.", nameof(cancellationToken));
        }

        var stdoutStream = await kubernetesService.GetLogStreamAsync(resource, Logs.StreamTypeStdOut, follow: true, cancellationToken).ConfigureAwait(false);
        var stderrStream = await kubernetesService.GetLogStreamAsync(resource, Logs.StreamTypeStdErr, follow: true, cancellationToken).ConfigureAwait(false);

        var channel = Channel.CreateUnbounded<(string Content, bool IsErrorMessage)>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });

        _ = Task.Run(() => streamLogs(stdoutStream, isError: false), cancellationToken);
        _ = Task.Run(() => streamLogs(stderrStream, isError: true), cancellationToken);

        await foreach (var batch in channel.GetBatches(cancellationToken))
        {
            yield return batch;
        }

        async Task streamLogs(Stream stream, bool isError)
        {
            using StreamReader sr = new StreamReader(stream, leaveOpen: false);
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                {
                    return; // No more data
                }
                channel.Writer.TryWrite((line, isError));
            }
        }
    }
}
