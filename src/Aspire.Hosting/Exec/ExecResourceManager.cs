// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Backchannel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Exec;

internal class ExecResourceManager
{
    private readonly ILogger _logger;
    private readonly ExecOptions _execOptions;
    private readonly DistributedApplicationModel _model;

    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;

    private readonly TaskCompletionSource<IResource> _execResourceInitialized = new();

    public ExecResourceManager(
        ILogger<ExecResourceManager> logger,
        IOptions<ExecOptions> execOptions,
        DistributedApplicationModel model,
        ResourceLoggerService resourceLoggerService,
        ResourceNotificationService resourceNotificationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _execOptions = execOptions.Value;

        _resourceLoggerService = resourceLoggerService ?? throw new ArgumentNullException(nameof(resourceLoggerService));
        _resourceNotificationService = resourceNotificationService ?? throw new ArgumentNullException(nameof(resourceNotificationService));
    }

    public async IAsyncEnumerable<CommandOutput> StreamExecResourceLogs([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_execOptions.Enabled)
        {
            yield break;
        }

        string type = "waiting";

        yield return new CommandOutput
        {
            Text = $"Waiting for resources to be initialized...",
            Type = type
        };

        // wait until AppHost eventing fires ConfigureExecResource()
        // and execResource is initialized
        IResource execResource;
        try
        {
            execResource = await _execResourceInitialized.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cancelled before exec resource was initialized.");
            yield break;
        }

        // we need to make sure resource is starting to be launched, and then we fetch the DCP name
        await _resourceNotificationService.WaitForResourceAsync(execResource.Name, targetState: KnownResourceStates.Starting, cancellationToken).ConfigureAwait(false);
        var dcpExecResourceName = GetDcpExecResourceName(execResource);

        yield return new CommandOutput
        {
            Text = $"Aspire exec starting...",
            Type = type
        };

        // in the background wait for the exec resource to be running to change log type
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(execResource.Name, targetState: KnownResourceStates.Running, cancellationToken).ConfigureAwait(false);
            type = "running";
        }, cancellationToken);

        // in the background wait for the exec resource to reach terminal state. Once done we can complete logging
        _ = Task.Run(async () =>
        {
            await _resourceNotificationService.WaitForResourceAsync(execResource.Name, targetStates: KnownResourceStates.TerminalStates, cancellationToken).ConfigureAwait(false);
            _resourceLoggerService.Complete(dcpExecResourceName); // complete stops the `WatchAsync` async-foreach below
        }, cancellationToken);

        await foreach (var logs in _resourceLoggerService.WatchAsync(dcpExecResourceName).WithCancellation(cancellationToken).ConfigureAwait(false))
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

    public IResource? ConfigureExecResource()
    {
        if (!_execOptions.Enabled)
        {
            return null;
        }

        var targetResource = _model.Resources.FirstOrDefault(x => x.Name.Equals(_execOptions.ResourceName, StringComparisons.ResourceName));
        if (targetResource is not IResourceSupportsExec targetExecResource)
        {
            _logger.LogWarning("Target resource '{ResourceName}' does not support exec.", _execOptions.ResourceName);
            throw new ArgumentException($"Target resource '{_execOptions.ResourceName}' does not support exec");
        }

        var execResource = BuildResource(targetExecResource);

        _logger.LogInformation("Resource '{ResourceName}' has been successfully built and added to the model resources.", execResource.Name);
        _execResourceInitialized.SetResult(execResource);
        return execResource;
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

        _logger.LogInformation("Exec resource '{ResourceName}' will run command '{Command}' with {ArgsCount} args '{Args}'.", execResourceName, exe, args?.Length ?? 0, string.Join(' ', args ?? []));

        return executable;

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

    private static string GetDcpExecResourceName(IResource resource)
    {
        if (!resource.TryGetLastAnnotation<DcpInstancesAnnotation>(out var dcpInstances))
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' does not have DCP instances annotation.");
        }
        if (dcpInstances.Instances.Length > 1)
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' has multiple DCP instances, expected only one. Instances: {string.Join(", ", dcpInstances.Instances.Select(i => i.Name))}");
        }

        return dcpInstances.Instances[0].Name;
    }
}
