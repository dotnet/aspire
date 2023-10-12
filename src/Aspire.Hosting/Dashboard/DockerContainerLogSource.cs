// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dashboard;

internal sealed class DockerContainerLogSource(string containerId) : ILogSource
{
    private readonly string _containerId = containerId;
    private DockerContainerLogWatcher? _containerLogWatcher;

    public async ValueTask<bool> StartAsync(CancellationToken cancellationToken)
    {
        if (_containerLogWatcher is not null)
        {
            return true;
        }

        _containerLogWatcher = new DockerContainerLogWatcher(_containerId);
        var watcherInitialized = await _containerLogWatcher.InitWatchAsync(cancellationToken).ConfigureAwait(false);
        if (!watcherInitialized)
        {
            await _containerLogWatcher.DisposeAsync().ConfigureAwait(false);
        }
        return watcherInitialized;
    }

    public async IAsyncEnumerable<string[]> WatchOutputLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_containerLogWatcher is not null)
        {
            await foreach (var logs in _containerLogWatcher!.WatchOutputLogsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return logs;
            }
        }
    }

    public async IAsyncEnumerable<string[]> WatchErrorLogAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_containerLogWatcher is not null)
        {
            await foreach (var logs in _containerLogWatcher!.WatchErrorLogsAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return logs;
            }
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (_containerLogWatcher is not null)
        {
            await _containerLogWatcher.DisposeAsync().ConfigureAwait(false);
            _containerLogWatcher = null;
        }
    }

    private sealed class DockerContainerLogWatcher(string? containerId) : IAsyncDisposable
    {
        private const string Executable = "docker";

        private readonly Channel<string> _outputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true });
        private readonly Channel<string> _errorChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true });

        private IAsyncDisposable? _processDisposable;

        public async Task<bool> InitWatchAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(containerId))
            {
                return false;
            }

            Task<ProcessResult>? processResultTask = null;

            try
            {
                var args = $"logs --follow -t {containerId}";

                var spec = new ProcessSpec(FileUtil.FindFullPathFromPath(Executable))
                {
                    Arguments = args,
                    OnOutputData = WriteToOutputChannel,
                    OnErrorData = WriteToErrorChannel,
                    KillEntireProcessTree = false,
                    ThrowOnNonZeroReturnCode = false // We don't want this to throw an exception because it is common
                                                     // for us to cancel the task and kill the process, which returns -1
                };

                (processResultTask, _processDisposable) = ProcessUtil.Run(spec);

                var tcs = new TaskCompletionSource();

                // Make sure the process exits if the cancellation token is cancelled
                var ctr = cancellationToken.Register(() => tcs.TrySetResult());

                // Don't forward cancellationToken here, because its handled internally in WaitForExit
                _ = Task.Run(() => WaitForExit(tcs, ctr), CancellationToken.None);

                return true;
            }
            catch
            {
                await DisposeProcess().ConfigureAwait(false);

                return false;
            }

            async ValueTask DisposeProcess()
            {
                if (_processDisposable is not null)
                {
                    await _processDisposable.DisposeAsync().ConfigureAwait(false);
                    _processDisposable = null;
                }
            }

            void WriteToOutputChannel(string s)
            {
                _outputChannel.Writer.TryWrite(s);
            }

            void WriteToErrorChannel(string s)
            {
                _errorChannel.Writer.TryWrite(s);
            }

            async Task WaitForExit(TaskCompletionSource tcs, CancellationTokenRegistration ctr)
            {
                if (processResultTask is not null)
                {
                    // Wait for cancellation (tcs.Task) or for the process itself to exit.
                    await Task.WhenAny(tcs.Task, processResultTask).ConfigureAwait(false);

                    if (processResultTask.IsCompleted)
                    {
                        // If it was the process that exited, write that out to the logs. If it was cancelled,
                        // there's no need to because the user has left the page
                        var processResult = processResultTask.Result;
                        await _outputChannel.Writer.WriteAsync($"Process exited with code {processResult.ExitCode}", cancellationToken).ConfigureAwait(false);
                    }

                    _outputChannel.Writer.Complete();
                    _errorChannel.Writer.Complete();

                    // If the process has already exited, this will be a no-op. But if it was cancelled
                    // we need to end the process
                    await DisposeProcess().ConfigureAwait(false);
                }

                ctr.Unregister();
            }
        }

        public async IAsyncEnumerable<string[]> WatchOutputLogsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<string> currentLogs = [];

                // Wait until there's something to read
                if (await _outputChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    // And then read everything that there is to read
                    while (!cancellationToken.IsCancellationRequested && _outputChannel.Reader.TryRead(out var log))
                    {
                        currentLogs.Add(log);
                    }

                    if (!cancellationToken.IsCancellationRequested && currentLogs.Count > 0)
                    {
                        yield return currentLogs.ToArray();
                    }
                }
                else
                {
                    // WaitToReadAsync will return false when the Channel is marked Complete
                    // down in WaitForExist, so we'll break out of the loop here
                    break;
                }
            }
        }

        public async IAsyncEnumerable<string[]> WatchErrorLogsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<string> currentLogs = [];

                // Wait until there's something to read
                if (await _errorChannel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    // And then read everything that there is to read
                    while (!cancellationToken.IsCancellationRequested && _errorChannel.Reader.TryRead(out var log))
                    {
                        currentLogs.Add(log);
                    }

                    if (!cancellationToken.IsCancellationRequested && currentLogs.Count > 0)
                    {
                        yield return currentLogs.ToArray();
                    }
                }
                else
                {
                    // WaitToReadAsync will return false when the Channel is marked Complete
                    // down in WaitForExist, so we'll break out of the loop here
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_processDisposable is not null)
            {
                await _processDisposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
