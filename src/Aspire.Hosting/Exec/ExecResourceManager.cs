// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourceManager : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ExecOptions _execOptions;
    private readonly DistributedApplicationModel _model;

    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;

    private string? _execResourceName;
    private IResource? _execResource;

    private static bool RunningInExecMode => true; // todo

    public ExecResourceManager(
        ILogger<ExecResourceManager> logger,
        IOptions<ExecOptions> execOptions,
        DistributedApplicationModel model,
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _execOptions = execOptions.Value;
        _model = model;

        _resourceLoggerService = resourceLoggerService;
        _resourceNotificationService = resourceNotificationService;
    }

    public async IAsyncEnumerable<IReadOnlyList<LogLine>> StreamExecResourceLogs([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!RunningInExecMode)
        {
            yield break;
        }
        if (string.IsNullOrEmpty(_execResourceName) || _execResource is null)
        {
            _logger.LogInformation("Exec resource can't be determined.");
            yield break;
        }

        await _resourceNotificationService.WaitForResourceAsync(_execResourceName, targetState: KnownResourceStates.Starting, cancellationToken).ConfigureAwait(false);

        // waiting for the resource to reach terminal state and completing the log stream then
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(_execResourceName, targetStates: KnownResourceStates.TerminalStates, cancellationToken).ConfigureAwait(false);
            _resourceLoggerService.Complete(_execResource);
        }, cancellationToken);
        
        await foreach (var logs in _resourceLoggerService.WatchAsync(_execResource).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return logs;
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!RunningInExecMode)
        {
            return Task.CompletedTask;
        }

        var targetResource = _model.Resources.FirstOrDefault(x => x.Name.Equals(_execOptions.ResourceName, StringComparisons.ResourceName));
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            _logger.LogWarning("Target resource '{ResourceName}' does not support exec.", _execOptions.ResourceName);
            return Task.CompletedTask;
        }

        var execResource = BuildResource(targetExecResource);
        _execResource = execResource;
        _model.Resources.Add(execResource);
        _logger.LogInformation("Resource '{ResourceName}' has been successfully built and added to the model resources.", _execResourceName);

        return Task.CompletedTask;
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

        var shortId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _execResourceName = "exec" + shortId;

        var executable = new ExecutableResource(_execResourceName, exe, projectDir);

        if (!string.IsNullOrEmpty(args))
        {
            executable.Annotations.Add(new CommandLineArgsCallbackAnnotation((c) =>
            {
                c.Args.Add(args);
                return Task.CompletedTask;
            }));
        }

        // take all annotations from the project resource and apply to the executable
        foreach (var annotation in project.Annotations
            // .Where(x => x is EnvironmentAnnotation or ResourceRelationshipAnnotation)
            .Where(x => x is not DcpInstancesAnnotation) // cant take dcp instances because it breaks DCP startup
        )
        {
            // todo understand if a deep-copy is required
            executable.Annotations.Add(annotation);
        }

        if (_execOptions.StartResource)
        {
            _logger.LogInformation("Exec resource '{ResourceName}' will wait until project '{Project}' starts up.", _execResourceName, project.Name);
            executable.Annotations.Add(new WaitAnnotation(project, waitType: WaitType.WaitUntilHealthy));
        }

        return executable;

        (string exe, string args) ParseCommand()
        {
            var split = _execOptions.Command.Split(' ', count: 2);
            return (split[0].Trim('"'), split[1].Trim('"'));
        }
    }
}
