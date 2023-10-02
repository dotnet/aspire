// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Aspire.Dashboard.Model;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Dashboard;

internal sealed class DockerContainerLogSource : IContainerLogSource
{
    public string? ContainerID { get; init; }

    public IContainerLogWatcher GetWatcher() => new DockerContainerLogWatcher(ContainerID);

    internal sealed class DockerContainerLogWatcher(string? containerID) : IContainerLogWatcher
    {
        private const string Executable = "docker";

        private readonly Channel<string> _outputChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true });
        private readonly Channel<string> _errorChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true });

        private IAsyncDisposable? _processDisposable;

        public async Task<bool> InitWatchAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(containerID))
            {
                return false;
            }

            Task<ProcessResult>? processResultTask = null;

            try
            {
                var args = $"logs --follow -t {containerID}";
                var output = new StringBuilder();

                var spec = new ProcessSpec(FileUtil.FindFullPathFromPath(Executable))
                {
                    Arguments = args,
                    OnOutputData = WriteToOutputChannel,
                    OnErrorData = WriteToErrorChannel,
                    KillEntireProcessTree = false
                };

                (processResultTask, _processDisposable) = ProcessUtil.Run(spec);

                // Make sure the process exits if the cancellation token is cancelled
                cancellationToken.Register(async () =>
                {
                    await DisposeProcess().ConfigureAwait(false);
                });

                _ = Task.Run(WaitForExit, cancellationToken);

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
                _ = Task.Run(async () =>
                {
                    await _outputChannel.Writer.WriteAsync(s, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
            }

            void WriteToErrorChannel(string s)
            {
                _ = Task.Run(async () =>
                {
                    await _errorChannel.Writer.WriteAsync(s, cancellationToken).ConfigureAwait(false);
                }, cancellationToken);
            }

            async Task WaitForExit()
            {
                if (processResultTask is not null)
                {
                    var processResult = await processResultTask.ConfigureAwait(false);
                    await _outputChannel.Writer.WriteAsync($"Process exited with code {processResult.ExitCode}", cancellationToken).ConfigureAwait(false);
                    _outputChannel.Writer.Complete();
                    _errorChannel.Writer.Complete();
                }
            }
        }

        public async IAsyncEnumerable<string[]> WatchOutputLogsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<string> currentLogs = new();

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
                List<string> currentLogs = new();

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
