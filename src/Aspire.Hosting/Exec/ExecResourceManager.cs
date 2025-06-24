// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourceManager : BackgroundService
{
    private readonly ExecOptions _execOptions;
    private readonly DistributedApplicationModel _model;

    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly IDcpExecutor _dcpExecutor;

    private IResource? _execResource;

    //private bool RunningInExecMode => _execOptions.Operation is "exec";
    private static bool RunningInExecMode => true;

    public ExecResourceManager(
        IOptions<ExecOptions> execOptions,
        IDcpExecutor dcpExecutor,
        DistributedApplicationModel model,
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService)
    {
        _execOptions = execOptions.Value;
        _dcpExecutor = dcpExecutor;
        _model = model;

        _resourceLoggerService = resourceLoggerService;
        _resourceNotificationService = resourceNotificationService;
    }

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning disable IDE0060 // Remove unused parameter
    public async IAsyncEnumerable<IReadOnlyList<LogLine>> StreamExecResourceLogs([EnumeratorCancellation] CancellationToken cancellationToken)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#pragma warning restore CA1822 // Mark members as static
    {
        if (!RunningInExecMode)
        {
            yield break;
        }

        var name = "exec";
        await _resourceNotificationService.WaitForResourceAsync(name, targetState: KnownResourceStates.Starting, cancellationToken).ConfigureAwait(false);

        var resourceReference = _dcpExecutor.GetResource(name);
        await foreach (var logs in _resourceLoggerService.WatchAsync(resourceReference.DcpResourceName).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return logs;
        }
    }

    IResource BuildResource(IResourceSupportsExec targetExecResource)
    {
        return targetExecResource switch
        {
           ProjectResource prj => BuildAgainstProjectResource(prj),
            _ => throw new NotImplementedException(nameof(targetExecResource))
        };
    }

    private IResource BuildAgainstProjectResource(ProjectResource project)
    {
        var projectMetadata = project.GetProjectMetadata();
        var projectDir = Path.GetDirectoryName(projectMetadata.ProjectPath) ?? throw new InvalidOperationException("Project path is invalid.");
        var (exe, args) = ParseCommand();

        var executable = new ExecutableResource("exec", exe, projectDir);
        executable.Annotations.Add(new CommandLineArgsCallbackAnnotation((c) =>
        {
            c.Args.Add(args);
            return Task.CompletedTask;
        }));

        //// take all annotations from the project resource and apply to the executable
        //foreach (var annotation in project.Annotations)
        //{
        //    // todo understand if a deep-copy is required
        //    executable.Annotations.Add(annotation);
        //}

        // executable.Annotations.Add(new WaitAnnotation(project, waitType: WaitType.WaitUntilHealthy));

        return executable;

        (string exe, string args) ParseCommand()
        {
            var split = _execOptions.Command.Split(' ', count: 2);
            return (split[0].Trim('"'), split[1].Trim('"'));
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!RunningInExecMode)
        {
            return Task.CompletedTask;
        }

        var targetResource = _model.Resources.FirstOrDefault(x => x.Name == _execOptions.ResourceName);
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            return Task.CompletedTask;
        }

        var execResource = BuildResource(targetExecResource);
        _execResource = execResource;
        _model.Resources.Add(execResource);

        return Task.CompletedTask;
    }
}
