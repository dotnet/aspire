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
    private const string Executable = "docker";

    public string? ContainerID { get; init; }

    public async IAsyncEnumerable<string[]> WatchLogsAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ContainerID))
        {
            yield break;
        }

        Channel<string> channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions() { SingleReader = true });

        Task<ProcessResult>? processResultTask = null;
        IAsyncDisposable? disposable = null;

        try
        {
            var args = $"logs --follow -t {ContainerID}";
            var output = new StringBuilder();

            var spec = new ProcessSpec(FileUtil.FindFullPathFromPath(Executable))
            {
                Arguments = args,
                OnOutputData = WriteToChannel,
                OnErrorData = WriteToChannel,
                KillEntireProcessTree = false
            };

            (processResultTask, disposable) = ProcessUtil.Run(spec);

            _ = Task.Run(WaitForExit, cancellationToken);
        }
        catch
        {
            if (disposable is not null)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
                disposable = null;
            }

            yield break;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            List<string> currentLogs = new();

            // Wait until there's something to read
            if (await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                // And then read everything that there is to read
                while (!cancellationToken.IsCancellationRequested && channel.Reader.TryRead(out var log))
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

        if (disposable is not null)
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }

        void WriteToChannel(string s)
        {
            _ = Task.Run(async () =>
            {
                await channel.Writer.WriteAsync(s, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        async Task WaitForExit()
        {
            if (processResultTask is not null)
            {
                var processResult = await processResultTask.ConfigureAwait(false);
                await channel.Writer.WriteAsync($"Process exited with code {processResult.ExitCode}", cancellationToken).ConfigureAwait(false);
                channel.Writer.Complete();
            }
        }
    }

    
}
