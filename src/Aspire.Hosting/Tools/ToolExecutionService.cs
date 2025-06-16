// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tools;

internal class ToolExecutionService : BackgroundService
{
    private readonly ToolOptions _toolOptions;
    private readonly ILogger<ToolExecutionService> _logger;

    private readonly IHostApplicationLifetime _lifetime;
    private readonly IDistributedApplicationEventing _eventing;
    private readonly ICliRpcTarget _cliRpcTarget;
    private readonly DistributedApplicationModel _model;

    public ToolExecutionService(
        IOptions<ToolOptions> toolOptions,
        ILogger<ToolExecutionService> logger,
        IHostApplicationLifetime lifetime,
        IDistributedApplicationEventing eventing,
        ICliRpcTarget cliRpcTarget,
        DistributedApplicationModel model)
    {
        _logger = logger;
        _toolOptions = toolOptions.Value;

        _lifetime = lifetime;
        _eventing = eventing;
        _cliRpcTarget = cliRpcTarget;
        _model = model;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // _eventing.Subscribe<AfterResourcesCreatedEvent>(AfterResourcesCreatedCallback);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CommandOutput> ExecuteToolAndStreamOutputAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IResource? toolResource = _model.Resources.FirstOrDefault(r => r.Name == _toolOptions.Resource);
        if (toolResource is null)
        {
            throw new InvalidOperationException($"Tool resource '{_toolOptions.Resource}' not found in the distributed application model.");
        }
        if (toolResource is not ExecutableResource toolExecutableResource)
        {
            throw new NotSupportedException("Can't run tool which is not executable");
        }

        var commandLineArgs = await BuildCommandLineArgsAsync(toolResource).ConfigureAwait(false);

        // Channel now carries (string Data, bool IsError)
        var outputChannel = Channel.CreateUnbounded<(string Data, bool IsError)>();
        var sendingTasks = new ConcurrentBag<Task>();

        var processSpec = new ProcessSpec(toolExecutableResource.Command)
        {
            Arguments = commandLineArgs,
            WorkingDirectory = Path.GetDirectoryName(toolExecutableResource.WorkingDirectory),
            OnOutputData = data =>
            {
                if (!string.IsNullOrEmpty(data))
                {
                    var writeTask = outputChannel.Writer.WriteAsync((data, false), cancellationToken).AsTask();
                    sendingTasks.Add(writeTask);
                }
            },
            OnErrorData = data =>
            {
                if (!string.IsNullOrEmpty(data))
                {
                    var writeTask = outputChannel.Writer.WriteAsync((data, true), cancellationToken).AsTask();
                    sendingTasks.Add(writeTask);
                }
            }
        };

        var (processResultTask, disposable) = ProcessUtil.Run(processSpec);
        var processResult = await processResultTask.ConfigureAwait(false);

        // Wait for all output to be written into channel
        await Task.WhenAll(sendingTasks).ConfigureAwait(false);

        // No more output expected, close the channel
        outputChannel.Writer.Complete();

        // Stream output with IsError
        await foreach (var (data, isError) in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return new CommandOutput
            {
                Text = data,
                IsError = isError
            };
        }

        await disposable.DisposeAsync().ConfigureAwait(false);
    }

#pragma warning disable IDE0051 // Remove unused private members
    private async Task AfterResourcesCreatedCallback(AfterResourcesCreatedEvent e, CancellationToken cancellationToken)
#pragma warning restore IDE0051 // Remove unused private members
    {
        IResource? toolResource = e.Model.Resources.FirstOrDefault(r => r.Name == _toolOptions.Resource);
        if (toolResource is null)
        {
            throw new InvalidOperationException($"Tool resource '{_toolOptions.Resource}' not found in the distributed application model.");
        }
        if (toolResource is not ExecutableResource toolExecutableResource)
        {
            throw new NotSupportedException("Cant run tool which is not executable");
        }

        var commandLineArgs = await BuildCommandLineArgsAsync(toolResource).ConfigureAwait(false);

        var outputChannel = Channel.CreateUnbounded<string>();
        var sendingTasks = new ConcurrentBag<Task>();

        var processSpec = new ProcessSpec(toolExecutableResource.Command)
        {
            Arguments = commandLineArgs,
            WorkingDirectory = Path.GetDirectoryName(toolExecutableResource.WorkingDirectory),
            OnOutputData = data =>
            {
                // Only write if data is not null or empty (defensive, not strictly needed for Action<string>)
                if (!string.IsNullOrEmpty(data))
                {
                    var writeTask = outputChannel.Writer.WriteAsync(data, cancellationToken).AsTask();
                    sendingTasks.Add(writeTask);
                }
            },
            OnErrorData = data =>
            {
                if (!string.IsNullOrEmpty(data))
                {
                    var writeTask = outputChannel.Writer.WriteAsync(data, cancellationToken).AsTask();
                    sendingTasks.Add(writeTask);
                }
            }
        };

        var (processResultTask, disposable) = ProcessUtil.Run(processSpec);
        var streamingOutputTask = Task.Run(async () =>
        {
            await foreach (var data in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await _cliRpcTarget.SendCommandOutputAsync(data, cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);

        var processResult = await processResultTask.ConfigureAwait(false);
        await Task.WhenAll(sendingTasks).ConfigureAwait(false);

        // no more output expected, closing and making sure all output is sent
        outputChannel.Writer.Complete();
        await streamingOutputTask.ConfigureAwait(false);

        // safe to dispose the process and stop the app host
        await disposable.DisposeAsync().ConfigureAwait(false);
        _lifetime.StopApplication();
    }

    private async Task<string> BuildCommandLineArgsAsync(IResource resource)
    {
        // create context to build command line arguments
        var context = new CommandLineArgsCallbackContext(new List<object>());

        // from apphost explicit args
        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var args))
        {
            foreach (var annotation in args)
            {
                if (annotation.Callback is null)
                {
                    continue;
                }

                await annotation.Callback(context).ConfigureAwait(false);
            }
        }

        // Attach args passed to `DistributedApplication` if present
        if (_toolOptions.Args is { Length: > 0 })
        {
            foreach (var arg in _toolOptions.Args)
            {
                context.Args.Add(arg);
            }
        }

        return string.Join(" ", context.Args.Select(x => x.ToString()));
    }
}
