// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting.Dcp;

internal class ResourceSnapshotBuilder
{
    private readonly DcpResourceState _resourceState;

    public ResourceSnapshotBuilder(DcpResourceState resourceState)
    {
        _resourceState = resourceState;
    }

    public CustomResourceSnapshot ToSnapshot(Container container, CustomResourceSnapshot previous)
    {
        var containerId = container.Status?.ContainerId;
        var urls = GetUrls(container, container.Status?.State);
        var volumes = GetVolumes(container);

        var environment = GetEnvironmentVariables(container.Status?.EffectiveEnv ?? container.Spec.Env, container.Spec.Env);
        var state = container.Status?.State;

        if (container.Spec.Start == false && (state == null || state == ContainerState.Pending))
        {
            state = KnownResourceStates.NotStarted;
        }

        var relationships = ImmutableArray<RelationshipSnapshot>.Empty;

        (ImmutableArray<string> Args, ImmutableArray<int>? ArgsAreSensitive, bool IsSensitive)? launchArguments = null;

        if (container.AppModelResourceName is not null &&
            _resourceState.ApplicationModel.TryGetValue(container.AppModelResourceName, out var appModelResource))
        {
            relationships = ApplicationModel.ResourceSnapshotBuilder.BuildRelationships(appModelResource);
            launchArguments = GetLaunchArgs(container);
        }

        return previous with
        {
            ResourceType = KnownResourceTypes.Container,
            State = state,
            // Map a container exit code of -1 (unknown) to null
            ExitCode = container.Status?.ExitCode is null or Conventions.UnknownExitCode ? null : container.Status.ExitCode,
            Properties = previous.Properties.SetResourcePropertyRange([
                new(KnownProperties.Container.Image, container.Spec.Image),
                new(KnownProperties.Container.Id, containerId),
                new(KnownProperties.Container.Command, container.Spec.Command),
                new(KnownProperties.Container.Args, container.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                new(KnownProperties.Container.Ports, GetPorts()),
                new(KnownProperties.Container.Lifetime, GetContainerLifetime()),
                new(KnownProperties.Resource.AppArgs, launchArguments?.Args) { IsSensitive = launchArguments?.IsSensitive ?? false },
                new(KnownProperties.Resource.AppArgsSensitivity, launchArguments?.ArgsAreSensitive) { IsSensitive = launchArguments?.IsSensitive ?? false },
            ]),
            EnvironmentVariables = environment,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToUniversalTime(),
            StartTimeStamp = container.Status?.StartupTimestamp?.ToUniversalTime(),
            StopTimeStamp = container.Status?.FinishTimestamp?.ToUniversalTime(),
            Urls = urls,
            Volumes = volumes,
            Relationships = relationships
        };

        ImmutableArray<int> GetPorts()
        {
            if (container.Spec.Ports is null)
            {
                return [];
            }

            var ports = ImmutableArray.CreateBuilder<int>();
            foreach (var port in container.Spec.Ports)
            {
                if (port.ContainerPort != null)
                {
                    ports.Add(port.ContainerPort.Value);
                }
            }
            return ports.ToImmutable();
        }

        ContainerLifetime GetContainerLifetime()
        {
            return (container.Spec.Persistent ?? false) ? ContainerLifetime.Persistent : ContainerLifetime.Session;
        }
    }

    public CustomResourceSnapshot ToSnapshot(Executable executable, CustomResourceSnapshot previous)
    {
        string? projectPath = null;
        IResource? appModelResource = null;

        if (executable.AppModelResourceName is not null &&
            _resourceState.ApplicationModel.TryGetValue(executable.AppModelResourceName, out appModelResource))
        {
            projectPath = appModelResource is ProjectResource p ? p.GetProjectMetadata().ProjectPath : null;
        }

        var state = executable.AppModelInitialState is "Hidden" ? "Hidden" : executable.Status?.State;

        var urls = GetUrls(executable, executable.Status?.State);

        var environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env);

        var relationships = ImmutableArray<RelationshipSnapshot>.Empty;
        if (appModelResource != null)
        {
            relationships = ApplicationModel.ResourceSnapshotBuilder.BuildRelationships(appModelResource);
        }

        var launchArguments = GetLaunchArgs(executable);

        if (projectPath is not null)
        {
            return previous with
            {
                ResourceType = KnownResourceTypes.Project,
                State = state,
                ExitCode = executable.Status?.ExitCode,
                Properties = previous.Properties.SetResourcePropertyRange([
                    new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                    new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                    new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                    new(KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                    new(KnownProperties.Project.Path, projectPath),
                    new(KnownProperties.Resource.AppArgs, launchArguments?.Args) { IsSensitive = launchArguments?.IsSensitive ?? false },
                    new(KnownProperties.Resource.AppArgsSensitivity, launchArguments?.ArgsAreSensitive) { IsSensitive = launchArguments?.IsSensitive ?? false },
                ]),
                EnvironmentVariables = environment,
                CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToUniversalTime(),
                StartTimeStamp = executable.Status?.StartupTimestamp?.ToUniversalTime(),
                StopTimeStamp = executable.Status?.FinishTimestamp?.ToUniversalTime(),
                Urls = urls,
                Relationships = relationships
            };
        }

        return previous with
        {
            ResourceType = KnownResourceTypes.Executable,
            State = state,
            ExitCode = executable.Status?.ExitCode,
            Properties = previous.Properties.SetResourcePropertyRange([
                new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                new(KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                new(KnownProperties.Resource.AppArgs, launchArguments?.Args) { IsSensitive = launchArguments?.IsSensitive ?? false },
                new(KnownProperties.Resource.AppArgsSensitivity, launchArguments?.ArgsAreSensitive) { IsSensitive = launchArguments?.IsSensitive ?? false },
            ]),
            EnvironmentVariables = environment,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToUniversalTime(),
            StartTimeStamp = executable.Status?.StartupTimestamp?.ToUniversalTime(),
            StopTimeStamp = executable.Status?.FinishTimestamp?.ToUniversalTime(),
            Urls = urls,
            Relationships = relationships
        };
    }

    private static (ImmutableArray<string> Args, ImmutableArray<int>? ArgsAreSensitive, bool IsSensitive)? GetLaunchArgs(CustomResource resource)
    {
        if (!resource.TryGetAnnotationAsObjectList(CustomResource.ResourceAppArgsAnnotation, out List<AppLaunchArgumentAnnotation>? launchArgumentAnnotations))
        {
            return null;
        }

        var launchArgsBuilder = ImmutableArray.CreateBuilder<string>();
        var argsAreSensitiveBuilder = ImmutableArray.CreateBuilder<int>();

        var anySensitive = false;
        foreach (var annotation in launchArgumentAnnotations)
        {
            if (annotation.IsSensitive)
            {
                anySensitive = true;
            }

            launchArgsBuilder.Add(annotation.Argument);
            argsAreSensitiveBuilder.Add(Convert.ToInt32(annotation.IsSensitive));
        }

        return (launchArgsBuilder.ToImmutable(), argsAreSensitiveBuilder.ToImmutable(), anySensitive);
    }

    private ImmutableArray<UrlSnapshot> GetUrls(CustomResource resource, string? resourceState)
    {
        var urls = ImmutableArray.CreateBuilder<UrlSnapshot>();
        var appModelResourceName = resource.AppModelResourceName;

        if (appModelResourceName is string resourceName &&
            _resourceState.ApplicationModel.TryGetValue(resourceName, out var appModelResource) &&
            appModelResource.TryGetUrls(out var resourceUrls))
        {
            var endpointUrls = resourceUrls.Where(u => u.Endpoint is not null).ToList();
            var nonEndpointUrls = resourceUrls.Where(u => u.Endpoint is null).ToList();

            var resourceServices = _resourceState.AppResources.OfType<ServiceAppResource>().Where(r => r.Service.AppModelResourceName == resource.AppModelResourceName).Select(s => s.Service).ToList();
            var name = resource.Metadata.Name;

            // Add the endpoint URLs
            var serviceEndpoints = new HashSet<(string EndpointName, string ServiceMetadataName)>(resourceServices.Where(s => !string.IsNullOrEmpty(s.EndpointName)).Select(s => (s.EndpointName!, s.Metadata.Name)));
            foreach (var endpoint in serviceEndpoints)
            {
                var (endpointName, serviceName) = endpoint;
                var urlsForEndpoint = endpointUrls.Where(u => string.Equals(endpointName, u.Endpoint?.EndpointName, StringComparisons.EndpointAnnotationName)).ToList();

                foreach (var endpointUrl in urlsForEndpoint)
                {
                    var activeEndpoint = _resourceState.EndpointsMap.SingleOrDefault(e => e.Value.Spec.ServiceName == serviceName && e.Value.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == name) == true).Value;
                    var isInactive = activeEndpoint is null;

                    urls.Add(new(Name: endpointUrl.Endpoint!.EndpointName, Url: endpointUrl.Url, IsInternal: endpointUrl.IsInternal) { IsInactive = isInactive, DisplayProperties = new(endpointUrl.DisplayText ?? "", endpointUrl.DisplayOrder ?? 0) });
                }
            }

            // Add the non-endpoint URLs
            var resourceRunning = string.Equals(resourceState, KnownResourceStates.Running, StringComparisons.ResourceState);
            foreach (var url in nonEndpointUrls)
            {
                urls.Add(new(Name: null, Url: url.Url, IsInternal: url.IsInternal) { IsInactive = !resourceRunning, DisplayProperties = new(url.DisplayText ?? "", url.DisplayOrder ?? 0) });
            }
        }

        return urls.ToImmutable();
    }

    private static ImmutableArray<VolumeSnapshot> GetVolumes(CustomResource resource)
    {
        if (resource is Container container)
        {
            return container.Spec.VolumeMounts?.Select(v => new VolumeSnapshot(v.Source, v.Target ?? "", v.Type, v.IsReadOnly)).ToImmutableArray() ?? [];
        }

        return [];
    }

    private static ImmutableArray<EnvironmentVariableSnapshot> GetEnvironmentVariables(List<EnvVar>? effectiveSource, List<EnvVar>? specSource)
    {
        if (effectiveSource is null or { Count: 0 })
        {
            return [];
        }

        var environment = ImmutableArray.CreateBuilder<EnvironmentVariableSnapshot>(effectiveSource.Count);

        foreach (var env in effectiveSource)
        {
            if (env.Name is not null)
            {
                var isFromSpec = specSource?.Any(e => string.Equals(e.Name, env.Name, StringComparison.Ordinal)) is true or null;

                environment.Add(new(env.Name, env.Value ?? "", isFromSpec));
            }
        }

        environment.Sort((v1, v2) => string.Compare(v1.Name, v2.Name, StringComparison.Ordinal));

        return environment.ToImmutable();
    }
}
