// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Dcp.Process;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tools;

internal class ToolExecutionService
{
    private readonly ToolOptions _toolOptions;
    private readonly ILogger<ToolExecutionService> _logger;
    private readonly DistributedApplicationModel _model;

    public ToolExecutionService(
        IOptions<ToolOptions> toolOptions,
        ILogger<ToolExecutionService> logger,
        DistributedApplicationModel model)
    {
        _logger = logger;
        _toolOptions = toolOptions.Value;
        _model = model;
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

        _logger.LogDebug("Starting tool execution: {Command} at {WorkingDir} with args {Args}", processSpec.ExecutablePath, processSpec.WorkingDirectory, processSpec.Arguments);
        var (processResultTask, disposable) = ProcessUtil.Run(processSpec);
        var processWatcherTask = Task.Run(async () =>
        {
            try
            {
                await processResultTask.ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                outputChannel.Writer.TryWrite(("failure:" + ex.Message, true));
                _logger.LogError(ex, "Process {executable} ended with exception", processSpec.ExecutablePath);
            }
            finally
            {
                await Task.WhenAll(sendingTasks).ConfigureAwait(false);
                outputChannel.Writer.Complete();
            }
        }, cancellationToken);

        await foreach (var (data, isError) in outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return new CommandOutput
            {
                Text = data,
                IsError = isError
            };
        }

        await processWatcherTask.ConfigureAwait(false);
        _logger.LogDebug("Finished tool execution: {Command} at {WorkingDir} with args {Args}", processSpec.ExecutablePath, processSpec.WorkingDirectory, processSpec.Arguments);
        await disposable.DisposeAsync().ConfigureAwait(false);
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
