// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Extensions;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dashboard;

internal sealed class DockerContainerLogSource(string containerId) : IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>
{
    public async IAsyncEnumerator<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        Task<ProcessResult>? processResultTask = null;
        IAsyncDisposable? processDisposable = null;

        var channel = Channel.CreateUnbounded<(string Content, bool IsErrorMessage)>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });

        try
        {
            var spec = new ProcessSpec(FileUtil.FindFullPathFromPath("docker"))
            {
                Arguments = $"logs --follow -t {containerId}",
                OnOutputData = OnOutputData,
                OnErrorData = OnErrorData,
                KillEntireProcessTree = false,
                // We don't want this to throw an exception because it is common for
                // us to cancel the task and kill the process, which returns -1.
                ThrowOnNonZeroReturnCode = false
            };

            (processResultTask, processDisposable) = ProcessUtil.Run(spec);

            var tcs = new TaskCompletionSource();

            // Make sure the process exits if the cancellation token is cancelled
            var ctr = cancellationToken.Register(() => tcs.TrySetResult());

            // Don't forward cancellationToken here, because it's handled internally in WaitForExit
            _ = Task.Run(() => WaitForExit(tcs, ctr), CancellationToken.None);

            await foreach (var batch in channel.GetBatches(cancellationToken))
            {
                yield return batch;
            }
        }
        finally
        {
            await DisposeProcess().ConfigureAwait(false);
        }

        yield break;

        void OnOutputData(string line)
        {
            channel.Writer.TryWrite((Content: line, IsErrorMessage: false));
        }

        void OnErrorData(string line)
        {
            channel.Writer.TryWrite((Content: line, IsErrorMessage: true));
        }

        async Task WaitForExit(TaskCompletionSource tcs, CancellationTokenRegistration ctr)
        {
            if (processResultTask is not null)
            {
                // Wait for cancellation (tcs.Task) or for the process itself to exit.
                await Task.WhenAny(tcs.Task, processResultTask).ConfigureAwait(false);

                if (processResultTask.IsCompleted)
                {
                    // If it was the process that exited, write that out to the logs.
                    // If it was cancelled, there's no need to because the user has left the page
                    var processResult = processResultTask.Result;
                    await channel.Writer.WriteAsync(($"Process exited with code {processResult.ExitCode}", false), cancellationToken).ConfigureAwait(false);
                }

                channel.Writer.Complete();

                // If the process has already exited, this will be a no-op. But if it was cancelled
                // we need to end the process
                await DisposeProcess().ConfigureAwait(false);
            }

            ctr.Unregister();
        }

        async ValueTask DisposeProcess()
        {
            if (processDisposable is not null)
            {
                await processDisposable.DisposeAsync().ConfigureAwait(false);
                processDisposable = null;
            }
        }
    }
}
