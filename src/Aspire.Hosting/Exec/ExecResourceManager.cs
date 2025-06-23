// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourceManager
{
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;

    private readonly ExecOptions _execOptions;
    private IResource? _execResource;

    //private bool RunningInExecMode => _execOptions.Operation is "exec";
    private static bool RunningInExecMode => true;

    public ExecResourceManager(
        IOptions<ExecOptions> execOptions,
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService)
    {
        _execOptions = execOptions.Value;
        _resourceLoggerService = resourceLoggerService;
        _resourceNotificationService = resourceNotificationService;
    }

    public IResource? GetExecResource() => _execResource;

    public async IAsyncEnumerable<IReadOnlyList<LogLine>> StreamExecResourceLogs([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!RunningInExecMode)
        {
            yield break;
        }

        await _resourceNotificationService.WaitForResourceAsync("exec", targetState: KnownResourceStates.Starting, cancellationToken).ConfigureAwait(false);
        await foreach (var logs in _resourceLoggerService.WatchAsync(_execResource!.Name).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return logs;
        }
    }

    public bool TryBuildExecResource(IResourceCollection resources, out IResource? execResource)
    {
        if (!RunningInExecMode)
        {
            execResource = null;
            return false;
        }

        var targetResource = resources.FirstOrDefault(x => x.Name == _execOptions.ResourceName);
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            execResource = null;
            return false;
        }

        execResource = BuildResource(targetExecResource);
        _execResource = execResource;
        return true;
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
        var executable = new ExecutableResource("exec", _execOptions.Command, projectMetadata.ProjectPath);

        //// take all annotations from the project resource and apply to the executable
        //foreach (var annotation in project.Annotations)
        //{
        //    // todo understand if a deep-copy is required
        //    executable.Annotations.Add(annotation);
        //}

        executable.Annotations.Add(new WaitAnnotation(project, waitType: WaitType.WaitUntilHealthy));

        return executable;
    }
}
