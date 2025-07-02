// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Aspire.Hosting.Dcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourceManager : BackgroundService
{
    private readonly ILogger _logger;
    private readonly ExecOptions _execOptions;
    private readonly DcpNameGenerator _dcpNameGenerator;
    private readonly DistributedApplicationModel _model;

    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;

    private string? _dcpExecResourceName;
    private IResource? _execResource;

    public ExecResourceManager(
        ILogger<ExecResourceManager> logger,
        IOptions<ExecOptions> execOptions,
        DcpNameGenerator dcpNameGenerator,
        DistributedApplicationModel model,
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _execOptions = execOptions.Value;

        _dcpNameGenerator = dcpNameGenerator ?? throw new ArgumentNullException(nameof(dcpNameGenerator));
        _model = model ?? throw new ArgumentNullException(nameof(model));

        _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
        _resourceNotificationService = resourceNotificationService ?? throw new ArgumentNullException(nameof(resourceNotificationService));
    }

    public async IAsyncEnumerable<CommandOutput> StreamExecResourceLogs([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_execOptions.Enabled)
        {
            yield break;
        }
        if (_execResource is null || string.IsNullOrEmpty(_dcpExecResourceName))
        {
            _logger.LogInformation("Exec resource can't be determined.");
            throw new ArgumentException($"Exec resource can't be determined.");
        }

        string type = "waiting";

        // wait for the exec resource to be running
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(_execResource.Name, targetState: KnownResourceStates.Running, cancellationToken).ConfigureAwait(false);
            type = "running";
        }, cancellationToken);

        // wait for the exec resource to reach terminal state
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(_execResource.Name, targetStates: KnownResourceStates.TerminalStates, cancellationToken).ConfigureAwait(false);
            _resourceLoggerService.Complete(_dcpExecResourceName); // complete stops the `WatchAsync` async-foreach below
        }, cancellationToken);
        
        await foreach (var logs in _resourceLoggerService.WatchAsync(_dcpExecResourceName).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            foreach (var log in logs)
            {
                yield return new CommandOutput
                {
                    Text = log.Content,
                    IsErrorMessage = log.IsErrorMessage,
                    LineNumber = log.LineNumber,
                    Type = type
                };
            }
        }
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_execOptions.Enabled)
        {
            return Task.CompletedTask;
        }

        var targetResource = _model.Resources.FirstOrDefault(x => x.Name.Equals(_execOptions.ResourceName, StringComparisons.ResourceName));
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            _logger.LogWarning("Target resource '{ResourceName}' does not support exec.", _execOptions.ResourceName);
            throw new ArgumentException($"Target resource '{_execOptions.ResourceName}' does not support exec");
        }

        var (execResource, dcpResourceName) = BuildResource(targetExecResource);
        _model.Resources.Add(execResource);

        _execResource = execResource;
        _dcpExecResourceName = dcpResourceName;
        _logger.LogInformation("Resource '{ResourceName}' has been successfully built and added to the model resources.", _execResource.Name);

        return Task.CompletedTask;
    }

    (IResource resource, string dcpResourceName) BuildResource(IResourceSupportsExec targetExecResource)
    {
        return targetExecResource switch
        {
            ProjectResource prj => BuildAgainstProjectResource(prj),
            _ => throw new NotImplementedException(nameof(targetExecResource))
        };
    }

    private (IResource resource, string dcpResourceName) BuildAgainstProjectResource(ProjectResource project)
    {
        var projectMetadata = project.GetProjectMetadata();
        var projectDir = Path.GetDirectoryName(projectMetadata.ProjectPath) ?? throw new InvalidOperationException("Project path is invalid.");
        var (exe, args) = ParseCommand();

        // unique name for the new exec resource
        string execResourceName;
        do
        {
            var shortId = Guid.NewGuid().ToString("N").Substring(0, 8);
            execResourceName = "exec" + shortId;
        } while (_model.Resources.Any(x => x.Name.Equals(execResourceName, StringComparisons.ResourceName)));

        var executable = new ExecutableResource(execResourceName, exe, projectDir);
        if (args is not null && args.Length > 0)
        {
            executable.Annotations.Add(new CommandLineArgsCallbackAnnotation((c) =>
            {
                c.Args.AddRange(args);
                return Task.CompletedTask;
            }));
        }

        // take all applicable annotations from target resource to replicate the environment
        foreach (var annotation in project.Annotations.Where(annotation =>
            annotation is EnvironmentAnnotation or EnvironmentCallbackAnnotation
                       or ResourceRelationshipAnnotation or WaitAnnotation))
        {
            executable.Annotations.Add(annotation);
        }

        if (_execOptions.StartResource)
        {
            _logger.LogInformation("Exec resource '{ResourceName}' will wait until project '{Project}' starts up.", execResourceName, project.Name);
            executable.Annotations.Add(new WaitAnnotation(project, waitType: WaitType.WaitUntilHealthy));
        }

        // in order to properly watch logs of the resource we need a dcp name, not the app-host model name,
        // so we need to prepare the DCP instances annotation as is done for any resource added to the DistributedApplicationBuilder
        var (dcpResourceName, suffix) = _dcpNameGenerator.GetExecutableName(executable);
        executable.Annotations.Add(new DcpInstancesAnnotation([new DcpInstance(dcpResourceName, suffix, 0)]));

        _logger.LogInformation("Exec resource '{ResourceName}' will run command '{Command}' with {ArgsCount} args '{Args}'.", dcpResourceName, exe, args?.Length ?? 0, string.Join(' ', args ?? []));

        return (executable, dcpResourceName);

        (string exe, string[] args) ParseCommand()
        {
            // cli wraps the command into the string with quotes
            // to keep the command as a single argument
            var command = _execOptions.Command;
            var commandUnwrapped = command.AsSpan(1, command.Length - 2).ToString();
            Debug.Assert(command[0] == '"' && command[^1] == '"');

            var split = commandUnwrapped.Split(' ', count: 2);
            var exe = split[0];
            string argsString = split.Length > 1 ? split[1] : string.Empty;

            string[] args = [];
            if (!string.IsNullOrEmpty(argsString))
            {
                args = argsString.Split(" ");
            }

            return (exe, args);
        }
    }
}
