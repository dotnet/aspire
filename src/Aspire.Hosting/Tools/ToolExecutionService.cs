// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Tools;

internal class ToolExecutionService : BackgroundService
{
    private readonly IDistributedApplicationEventing _eventing;
    private readonly ICliRpcTarget _cliRpcTarget;
    private readonly ToolOptions _toolOptions;

    public ToolExecutionService(
        IDistributedApplicationEventing eventing,
        ICliRpcTarget cliRpcTarget,
        IOptions<ToolOptions> toolOptions)
    {
        _eventing = eventing;
        _cliRpcTarget = cliRpcTarget;
        _toolOptions = toolOptions.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventing.Subscribe<AfterResourcesCreatedEvent>(AfterResourcesCreatedCallback);
        return Task.CompletedTask;
    }

    private async Task AfterResourcesCreatedCallback(AfterResourcesCreatedEvent e, CancellationToken token)
    {
        IResource? toolResource = e.Model.Resources.FirstOrDefault(r => r.Name == _toolOptions.Resource);
        if (toolResource is null)
        {
            throw new InvalidOperationException($"Tool resource '{_toolOptions.Resource}' not found in the distributed application model.");
        }
        if (toolResource is not ExecutableResource toolExecutableResource
            || !toolExecutableResource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var args))
        {
            throw new NotSupportedException("Cant run tool which is not executable");
        }

        var context = new CommandLineArgsCallbackContext(new List<object>());
        await args.First().Callback(context).ConfigureAwait(false);
        var commandLineArgs = string.Join(" ", context.Args.Select(x => x.ToString()));

        var processSpec = new ProcessSpec(toolExecutableResource.Command)
        {
            Arguments = commandLineArgs,
            WorkingDirectory = Path.GetDirectoryName(toolExecutableResource.WorkingDirectory),

            // put streaming on the separate thread to not block
            OnOutputData = (data) => { Task.Run(() => _cliRpcTarget.SendCommandOutputAsync(data, token)); },
            OnErrorData = (data) => { Task.Run(() => _cliRpcTarget.SendCommandErrorAsync(data, token)); }
        };

        var (processResultTask, disposable) = ProcessUtil.Run(processSpec);
        var processResult = await processResultTask.ConfigureAwait(false);
        await disposable.DisposeAsync().ConfigureAwait(false);

        return;
    }
}
