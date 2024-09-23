// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Orchestration;

internal class DistributedApplicationOrchestrator(ResourceNotificationService resourceNotificationService, ResourceLoggerService resourceLoggerService) : IDistributedApplicationOrchestrator
{
    private async Task WaitUntilHealthyAsync(IResource resource, IResource dependency, CancellationToken cancellationToken)
    {
        var resourceLogger = resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to enter the '{State}' state.", dependency.Name, KnownResourceStates.Running);

        await resourceNotificationService.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);
        var resourceEvent = await resourceNotificationService.WaitForResourceAsync(dependency.Name, re => IsContinuableState(re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
        var snapshot = resourceEvent.Snapshot;

        if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
        {
            resourceLogger.LogError(
                "Dependency resource '{ResourceName}' failed to start.",
                dependency.Name
                );

            throw new DistributedApplicationException($"Dependency resource '{dependency.Name}' failed to start.");
        }
        else if (snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited)
        {
            resourceLogger.LogError(
                "Resource '{ResourceName}' has entered the '{State}' state prematurely.",
                dependency.Name,
                snapshot.State.Text
                );

            throw new DistributedApplicationException(
                $"Resource '{dependency.Name}' has entered the '{snapshot.State.Text}' state prematurely."
                );
        }

        // If our dependency resource has health check annotations we want to wait until they turn healthy
        // otherwise we don't care about their health status.
        if (dependency.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var _))
        {
            resourceLogger.LogInformation("Waiting for resource '{Name}' to become healthy.", dependency.Name);
            await resourceNotificationService.WaitForResourceAsync(dependency.Name, re => re.Snapshot.HealthStatus == HealthStatus.Healthy, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", dependency.Name);

        static bool IsContinuableState(CustomResourceSnapshot snapshot) =>
            snapshot.State?.Text == KnownResourceStates.Running ||
            snapshot.State?.Text == KnownResourceStates.Finished ||
            snapshot.State?.Text == KnownResourceStates.Exited ||
            snapshot.State?.Text == KnownResourceStates.FailedToStart;
    }

    private async Task WaitUntilCompletionAsync(IResource resource, IResource dependency, int exitCode, CancellationToken cancellationToken)
    {
        if (dependency.TryGetLastAnnotation<ReplicaAnnotation>(out var replicaAnnotation) && replicaAnnotation.Replicas > 1)
        {
            throw new DistributedApplicationException("WaitForCompletion cannot be used with resources that have replicas.");
        }

        var resourceLogger = resourceLoggerService.GetLogger(resource);
        resourceLogger.LogInformation("Waiting for resource '{Name}' to complete.", dependency.Name);

        await resourceNotificationService.PublishUpdateAsync(resource, s => s with { State = KnownResourceStates.Waiting }).ConfigureAwait(false);
        var resourceEvent = await resourceNotificationService.WaitForResourceAsync(dependency.Name, re => IsKnownTerminalState(re.Snapshot), cancellationToken: cancellationToken).ConfigureAwait(false);
        var snapshot = resourceEvent.Snapshot;

        if (snapshot.State?.Text == KnownResourceStates.FailedToStart)
        {
            resourceLogger.LogError(
                "Dependency resource '{ResourceName}' failed to start.",
                dependency.Name
                );

            throw new DistributedApplicationException($"Dependency resource '{dependency.Name}' failed to start.");
        }
        else if ((snapshot.State!.Text == KnownResourceStates.Finished || snapshot.State!.Text == KnownResourceStates.Exited) && snapshot.ExitCode is not null && snapshot.ExitCode != exitCode)
        {
            resourceLogger.LogError(
                "Resource '{ResourceName}' has entered the '{State}' state with exit code '{ExitCode}'",
                dependency.Name,
                snapshot.State.Text,
                snapshot.ExitCode
                );

            throw new DistributedApplicationException(
                $"Resource '{dependency.Name}' has entered the '{snapshot.State.Text}' state with exit code '{snapshot.ExitCode}'"
                );
        }

        resourceLogger.LogInformation("Finished waiting for resource '{Name}'.", dependency.Name);

        static bool IsKnownTerminalState(CustomResourceSnapshot snapshot) =>
            KnownResourceStates.TerminalStates.Contains(snapshot.State?.Text) ||
            snapshot.ExitCode is not null;
    }

    public async Task WaitForDependenciesAsync(IResource resource, CancellationToken cancellationToken)
    {
        if (!resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
        {
            return;
        }

        var pendingDependencies = new List<Task>();
        foreach (var waitAnnotation in waitAnnotations)
        {
            var pendingDependency = waitAnnotation.WaitType switch
            {
                WaitType.WaitUntilHealthy => WaitUntilHealthyAsync(resource, waitAnnotation.Resource, cancellationToken),
                WaitType.WaitForCompletion => WaitUntilCompletionAsync(resource, waitAnnotation.Resource, waitAnnotation.ExitCode, cancellationToken),
                _ => throw new DistributedApplicationException($"Unexpected wait type: {waitAnnotation.WaitType}")
            };
            pendingDependencies.Add(pendingDependency);
        }

        await Task.WhenAll(pendingDependencies).ConfigureAwait(false);
    }
}
