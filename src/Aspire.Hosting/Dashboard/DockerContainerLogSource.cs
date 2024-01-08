// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Channels;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Extensions;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dashboard;

internal sealed class DockerContainerLogSource : IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>
{
    private readonly string _containerId;
    private readonly CancellationToken _cancellationToken;

    public DockerContainerLogSource(string containerId, CancellationToken cancellationToken)
    {
        _containerId = containerId;
        _cancellationToken = cancellationToken;
    }

    public IAsyncEnumerator<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationToken, cancellationToken);

        return GetAsyncEnumeratorCore(linkedCts);
    }

    public async IAsyncEnumerator<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetAsyncEnumeratorCore(CancellationTokenSource cts)
    {
        var cancellationToken = cts.Token;

        Task<ProcessResult>? processResultTask = null;
        IAsyncDisposable? processDisposable = null;

        var channel = Channel.CreateUnbounded<(string Content, bool IsErrorMessage)>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });

        try
        {
            var spec = new ProcessSpec(FileUtil.FindFullPathFromPath("docker"))
            {
                Arguments = $"logs --follow -t {_containerId}",
                OnOutputData = OnOutputData,
                OnErrorData = OnErrorData,
                KillEntireProcessTree = false,
                // We don't want this to throw an exception because it is common for
                // us to cancel the task and kill the process, which returns -1.
                ThrowOnNonZeroReturnCode = false
            };

            (processResultTask, processDisposable) = ProcessUtil.Run(spec);

            // Don't forward cancellationToken here, because it's handled internally in WaitForExit
            _ = Task.Run(WaitForProcessExitOrCancellationAsync, CancellationToken.None);

            await foreach (var batch in channel.GetBatchesAsync(cancellationToken))
            {
                yield return batch;
            }
        }
        finally
        {
            cts.Dispose();
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

        async Task WaitForProcessExitOrCancellationAsync()
        {
            Debug.Assert(processResultTask != null);

            var tcs = new TaskCompletionSource();

            // Make sure the process exits if the cancellation token is cancelled
            using var ctr = cancellationToken.Register(() => tcs.TrySetResult());

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
