// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal sealed class WatchAspireEventHandlers(
    IOptions<DcpOptions> options,
    ILogger<DcpExecutor> logger,
    DcpNameGenerator nameGenerator,
    DistributedApplicationOptions distributedApplicationOptions) : IDistributedApplicationEventingSubscriber
{
    internal const string WatchServerResourceName = "aspire-watch-server";

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (executionContext.IsRunMode)
        {
            eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        }

        return Task.CompletedTask;
    }

    private async Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken)
    {
        var watchAspirePath = options.Value.WatchAspirePath;
        logger.LogDebug("WatchAspirePath resolved to: {WatchAspirePath}", watchAspirePath ?? "(null)");
        if (watchAspirePath is null)
        {
            return;
        }

        // Collect all project resource paths (skip file-based apps) and build path → resource mapping
        var projectPaths = new List<string>();
        var projectPathToResource = new Dictionary<string, IResource>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in @event.Model.GetProjectResources())
        {
            if (project.TryGetLastAnnotation<IProjectMetadata>(out var metadata) && !metadata.IsFileBasedApp
                && !StringComparers.ResourceName.Equals(project.Name, KnownResourceNames.AspireDashboard))
            {
                projectPaths.Add(metadata.ProjectPath);
                projectPathToResource[metadata.ProjectPath] = project;
            }
        }

        if (projectPaths.Count == 0)
        {
            return;
        }

        // Resolve SDK path using `dotnet --version` from the AppHost project directory so global.json is respected
        var sdkPath = await DotnetSdkUtils.TryGetSdkDirectoryAsync(distributedApplicationOptions.ProjectDirectory).ConfigureAwait(false);
        if (sdkPath is null)
        {
            logger.LogWarning("Cannot resolve .NET SDK path. Watch.Aspire hot reload server will not be started.");
            return;
        }

        // Generate unique pipe names
        var serverPipeName = $"aw-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}";
        var statusPipeName = $"aws-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}";
        var controlPipeName = $"awc-{Environment.ProcessId}-{Guid.NewGuid().ToString("N")[..8]}";

        // Determine the working directory
        var cwd = Path.GetDirectoryName(watchAspirePath) ?? Directory.GetCurrentDirectory();

        // Resolve the DLL path - if the path is not a .dll, find the .dll next to it
        var watchAspireDllPath = watchAspirePath;
        if (!watchAspirePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            watchAspireDllPath = Path.ChangeExtension(watchAspirePath, ".dll");
        }

        // Create the watch server as a hidden ExecutableResource (following the Dashboard pattern)
        var watchServerResource = new ExecutableResource(WatchServerResourceName, "dotnet", cwd);

        watchServerResource.Annotations.Add(new CommandLineArgsCallbackAnnotation(args =>
        {
            args.Add("exec");
            args.Add(watchAspireDllPath);
            args.Add("server");
            args.Add("--server");
            args.Add(serverPipeName);
            args.Add("--sdk");
            args.Add(sdkPath);
            args.Add("--status-pipe");
            args.Add(statusPipeName);
            args.Add("--control-pipe");
            args.Add(controlPipeName);
            foreach (var projPath in projectPaths)
            {
                args.Add("--resource");
                args.Add(projPath);
            }
        }));

        nameGenerator.EnsureDcpInstancesPopulated(watchServerResource);

        // Mark as hidden and exclude lifecycle commands
        var snapshot = new CustomResourceSnapshot
        {
            Properties = [],
            ResourceType = watchServerResource.GetResourceType(),
            IsHidden = true
        };
        watchServerResource.Annotations.Add(new ResourceSnapshotAnnotation(snapshot));
        watchServerResource.Annotations.Add(new ExcludeLifecycleCommandsAnnotation());

        // Store pipe names and project mapping so DcpExecutor can find them
        watchServerResource.Annotations.Add(new WatchAspireAnnotation(
            serverPipeName, statusPipeName, controlPipeName, projectPathToResource));

        // Insert first so DCP starts it before project resources
        @event.Model.Resources.Insert(0, watchServerResource);

        logger.LogInformation("Watch.Aspire hot reload server enabled for {Count} project(s).", projectPaths.Count);
    }
}
