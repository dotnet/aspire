// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Data;
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
        var urls = GetUrls(container);
        var volumes = GetVolumes(container);

        var environment = GetEnvironmentVariables(container.Status?.EffectiveEnv ?? container.Spec.Env, container.Spec.Env);
        var state = container.AppModelInitialState == KnownResourceStates.Hidden ? KnownResourceStates.Hidden : container.Status?.State;

        var relationships = ImmutableArray<RelationshipSnapshot>.Empty;
        if (container.AppModelResourceName is not null &&
            _resourceState.ApplicationModel.TryGetValue(container.AppModelResourceName, out var appModelResource))
        {
            relationships = ApplicationModel.ResourceSnapshotBuilder.BuildRelationships(appModelResource);
        }

        return previous with
        {
            ResourceType = KnownResourceTypes.Container,
            State = state,
            // Map a container exit code of -1 (unknown) to null
            ExitCode = container.Status?.ExitCode is null or Conventions.UnknownExitCode ? null : container.Status.ExitCode,
            Properties = [
                new(KnownProperties.Container.Image, container.Spec.Image),
                new(KnownProperties.Container.Id, containerId),
                new(KnownProperties.Container.Command, container.Spec.Command),
                new(KnownProperties.Container.Args, container.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                new(KnownProperties.Container.Ports, GetPorts()),
                new(KnownProperties.Container.Lifetime, GetContainerLifetime()),
            ],
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

        var urls = GetUrls(executable);

        var environment = GetEnvironmentVariables(executable.Status?.EffectiveEnv, executable.Spec.Env);

        var relationships = ImmutableArray<RelationshipSnapshot>.Empty;
        if (appModelResource != null)
        {
            relationships = ApplicationModel.ResourceSnapshotBuilder.BuildRelationships(appModelResource);
        }

        if (projectPath is not null)
        {
            return previous with
            {
                ResourceType = KnownResourceTypes.Project,
                State = state,
                ExitCode = executable.Status?.ExitCode,
                Properties = [
                    new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                    new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                    new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                    new(KnownProperties.Executable.Pid, executable.Status?.ProcessId),
                    new(KnownProperties.Project.Path, projectPath)
                ],
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
            Properties = [
                new(KnownProperties.Executable.Path, executable.Spec.ExecutablePath),
                new(KnownProperties.Executable.WorkDir, executable.Spec.WorkingDirectory),
                new(KnownProperties.Executable.Args, executable.Status?.EffectiveArgs ?? []) { IsSensitive = true },
                new(KnownProperties.Executable.Pid, executable.Status?.ProcessId)
            ],
            EnvironmentVariables = environment,
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToUniversalTime(),
            StartTimeStamp = executable.Status?.StartupTimestamp?.ToUniversalTime(),
            StopTimeStamp = executable.Status?.FinishTimestamp?.ToUniversalTime(),
            Urls = urls,
            Relationships = relationships
        };
    }

    private ImmutableArray<UrlSnapshot> GetUrls(CustomResource resource)
    {
        var name = resource.Metadata.Name;

        var urls = ImmutableArray.CreateBuilder<UrlSnapshot>();

        foreach (var (_, endpoint) in _resourceState.EndpointsMap)
        {
            if (endpoint.Metadata.OwnerReferences?.Any(or => or.Kind == resource.Kind && or.Name == name) != true)
            {
                continue;
            }

            if (endpoint.Spec.ServiceName is not null &&
                _resourceState.ServicesMap.TryGetValue(endpoint.Spec.ServiceName, out var service) &&
                service.AppModelResourceName is string resourceName &&
                _resourceState.ApplicationModel.TryGetValue(resourceName, out var appModelResource) &&
                appModelResource is IResourceWithEndpoints resourceWithEndpoints &&
                service.EndpointName is string endpointName)
            {
                var ep = resourceWithEndpoints.GetEndpoint(endpointName);

                if (ep.EndpointAnnotation.FromLaunchProfile &&
                    appModelResource is ProjectResource p &&
                    p.GetEffectiveLaunchProfile()?.LaunchProfile is LaunchProfile profile &&
                    profile.LaunchUrl is string launchUrl)
                {
                    // Concat the launch url from the launch profile to the urls with IsFromLaunchProfile set to true

                    string CombineUrls(string url, string launchUrl)
                    {
                        if (!launchUrl.Contains("://"))
                        {
                            // This is relative URL
                            url += $"/{launchUrl}";
                        }
                        else
                        {
                            // For absolute URL we need to update the port value if possible
                            if (profile.ApplicationUrl is string applicationUrl
                                && launchUrl.StartsWith(applicationUrl))
                            {
                                url = launchUrl.Replace(applicationUrl, url);
                            }
                        }

                        return url;
                    }

                    if (ep.IsAllocated)
                    {
                        var url = CombineUrls(ep.Url, launchUrl);

                        urls.Add(new(Name: ep.EndpointName, Url: url, IsInternal: false));
                    }
                }
                else
                {
                    if (ep.IsAllocated)
                    {
                        urls.Add(new(Name: ep.EndpointName, Url: ep.Url, IsInternal: false));
                    }
                }

                if (ep.EndpointAnnotation.IsProxied)
                {
                    var endpointString = $"{ep.Scheme}://{endpoint.Spec.Address}:{endpoint.Spec.Port}";
                    urls.Add(new(Name: $"{ep.EndpointName} target port", Url: endpointString, IsInternal: true));
                }
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
