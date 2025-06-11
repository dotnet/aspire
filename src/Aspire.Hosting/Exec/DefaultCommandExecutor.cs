// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class DefaultCommandExecutor : IDistributedApplicationLifecycleHook
{
    private readonly ExecOptions _execOptions;

    public DefaultCommandExecutor(IOptions<ExecOptions> execOptions)
    {
        _execOptions = execOptions.Value ?? throw new ArgumentNullException(nameof(execOptions));
    }

    public async Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        // search for the target resource
        var targetResource = appModel.Resources.FirstOrDefault(r => r.Name == _execOptions.Resource);
        if (targetResource is null)
        {
            throw new InvalidOperationException($"Resource '{_execOptions.Resource}' not found in the application model.");
        }

        IProjectMetadata? projectMetadata;
        string? workingDirectory = null;
        if (targetResource.TryGetAnnotationsOfType<IProjectMetadata>(out var projectMetadatas)
            && (projectMetadata = projectMetadatas.FirstOrDefault()) is not null)
        {
            workingDirectory = Path.GetDirectoryName(projectMetadata!.ProjectPath);
        }
        
        Action<string>? onOutput = data =>
        {
            Console.WriteLine(data + "");
        };
        Action<string>? onError = data =>
        {
            Console.WriteLine(data + "");
        };

        // maybe we need to determine process from the first arg in command?
        // like command `dotnet run ...` where process is `dotnet`
        var commandSplit = _execOptions.Command.Split(' ', count: 2, StringSplitOptions.RemoveEmptyEntries);

        var processSpec = new ProcessSpec(commandSplit.First())
        {
            Arguments = commandSplit[1],
            WorkingDirectory = workingDirectory,
            OnErrorData = onError,
            OnOutputData = onOutput
        };
        var (processResult, processDisposable) = ProcessUtil.Run(processSpec);

        // catch and report?
        var result = await processResult.ConfigureAwait(false);
        await processDisposable.DisposeAsync().ConfigureAwait(false);

        // completed command execution
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{_execOptions.Command}' failed with exit code {result.ExitCode}.");
        }
    }
}
