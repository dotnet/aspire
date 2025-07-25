// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes.Resources;

namespace Aspire.Hosting.Kubernetes.Extensions;

internal static class ResourceExtensions
{
    internal static Deployment ToDeployment(this IResource resource, KubernetesResource context)
    {
        var deployment = new Deployment
        {
            Metadata =
            {
                Name = resource.Name.ToDeploymentName(),
                Labels = context.Labels.ToDictionary(),
            },
            Spec =
            {
                Selector = new(context.Labels.ToDictionary()),
                Replicas = resource.GetReplicaCount(),
                Template = resource.ToPodTemplateSpec(context),
                Strategy = new()
                {
                    Type = "RollingUpdate",
                    RollingUpdate = new()
                    {
                        MaxUnavailable = 1,
                        MaxSurge = 1,
                    },
                },
            },
        };

        return deployment;
    }

    internal static StatefulSet ToStatefulSet(this IResource resource, KubernetesResource context)
    {
        var statefulSet = new StatefulSet
        {
            Metadata =
            {
                Name = resource.Name.ToStatefulSetName(),
                Labels = context.Labels.ToDictionary(),
            },
            Spec =
            {
                Selector = new(context.Labels.ToDictionary()),
                Replicas = resource.GetReplicaCount(),
                Template = resource.ToPodTemplateSpec(context),
            },
        };

        return statefulSet;
    }

    internal static Secret? ToSecret(this IResource resource, KubernetesResource context)
    {
        if (context.Secrets.Count == 0)
        {
            return null;
        }

        var secret = new Secret
        {
            Metadata =
            {
                Name = resource.Name.ToSecretName(),
                Labels = context.Labels.ToDictionary(),
            },
        };

        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in context.Secrets.Where(kvp => !processedKeys.Contains(kvp.Key)))
        {
            // If the value itself contains Helm expressions, use it directly in the template
            // Otherwise use the expression to reference values.yaml
            secret.StringData[kvp.Key] = (kvp.Value.Value?.ContainsHelmExpression() == true)
                ? kvp.Value.Value
                : kvp.Value.HelmExpression;
            processedKeys.Add(kvp.Key);
        }

        return secret;
    }

    internal static ConfigMap? ToConfigMap(this IResource resource, KubernetesResource context)
    {
        if (context.EnvironmentVariables.Count == 0)
        {
            return null;
        }

        var configMap = new ConfigMap
        {
            Metadata =
            {
                Name = resource.Name.ToConfigMapName(),
                Labels = context.Labels.ToDictionary(),
            },
        };

        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in context.EnvironmentVariables.Where(kvp => !processedKeys.Contains(kvp.Key)))
        {
            configMap.Data[kvp.Key] = kvp.Value.HelmExpression;
            processedKeys.Add(kvp.Key);
        }

        return configMap;
    }

    internal static Service? ToService(this IResource resource, KubernetesResource context)
    {
        if (context.EndpointMappings.Count == 0)
        {
            return null;
        }

        var service = new Service
        {
            Metadata =
            {
                Name = resource.Name.ToServiceName(),
                Labels = context.Labels.ToDictionary(),
            },
            Spec =
            {
                Selector = context.Labels.ToDictionary(),
                Type = context.Parent.DefaultServiceType,
            },
        };

        foreach (var (_, mapping) in context.EndpointMappings)
        {
            service.Spec.Ports.Add(
                new()
                {
                    Name = mapping.Name,
                    Port = new(mapping.Port),
                    TargetPort = new(mapping.Port),
                    Protocol = "TCP",
                });
        }

        return service;
    }

    private static PodTemplateSpecV1 ToPodTemplateSpec(this IResource resource, KubernetesResource context)
    {
        var podTemplateSpec = new PodTemplateSpecV1
        {
            Metadata =
            {
                Labels = context.Labels.ToDictionary(),
            },
            Spec =
            {
                Containers =
                {
                    resource.ToContainerV1(context),
                },
            },
        };

        return podTemplateSpec.WithPodSpecVolumes(context);
    }

    private static PodTemplateSpecV1 WithPodSpecVolumes(this PodTemplateSpecV1 podTemplateSpec, KubernetesResource context)
    {
        if (context.Volumes.Count == 0)
        {
            return podTemplateSpec;
        }

        foreach (var volume in context.Volumes)
        {
            var podVolume = new VolumeV1
            {
                Name = volume.Name,
            };

            switch (context.Parent.DefaultStorageType.ToLowerInvariant())
            {
                case "emptydir":
                    podVolume.EmptyDir = new();
                    break;

                case "hostpath":
                    podVolume.HostPath = new()
                    {
                        Path = volume.MountPath,
                        Type = "Directory",
                    };
                    break;

                case "pvc":
                    _ = CreatePersistentVolume(context, volume);
                    var pvc = CreatePersistentVolumeClaim(context, volume);
                    podVolume.PersistentVolumeClaim = new()
                    {
                        ClaimName = pvc.Metadata.Name,
                    };
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported storage type: {context.Parent.DefaultStorageType}");
            }

            podTemplateSpec.Spec.Volumes.Add(podVolume);
        }

        return podTemplateSpec;
    }

    private static ContainerV1 ToContainerV1(this IResource resource, KubernetesResource context)
    {
        var container = new ContainerV1
        {
            Name = resource.Name,
            ImagePullPolicy = context.Parent.DefaultImagePullPolicy,
        };

        return container
            .WithContainerImage(context)
            .WithContainerEntrypoint(context)
            .WithContainerArgs(context)
            .WithContainerEnvironmentalVariables(context)
            .WithContainerSecrets(context)
            .WithContainerPorts(context)
            .WithContainerVolumes(context);
    }

    private static ContainerV1 WithContainerVolumes(this ContainerV1 container, KubernetesResource context)
    {
        if (context.Volumes.Count == 0)
        {
            return container;
        }

        foreach (var volume in context.Volumes)
        {
            container.VolumeMounts.Add(
                new()
                {
                    Name = volume.Name,
                    MountPath = volume.MountPath,
                });
        }

        return container;
    }

    private static ContainerV1 WithContainerPorts(this ContainerV1 container, KubernetesResource context)
    {
        if (context.EndpointMappings.Count == 0)
        {
            return container;
        }

        foreach (var (_, mapping) in context.EndpointMappings)
        {
            container.Ports.Add(
                new()
                {
                    Name = mapping.Name,
                    ContainerPort = new(mapping.Port),
                    Protocol = "TCP",
                });
        }

        return container;
    }

    private static ContainerV1 WithContainerImage(this ContainerV1 container, KubernetesResource context)
    {
        container.Image = context.GetContainerImageName(context.TargetResource);

        return container;
    }

    private static ContainerV1 WithContainerEntrypoint(this ContainerV1 container, KubernetesResource context)
    {
        if (context.TargetResource is ContainerResource { Entrypoint: { } entrypoint })
        {
            container.Command.Add(entrypoint);
        }

        return container;
    }

    private static ContainerV1 WithContainerArgs(this ContainerV1 container, KubernetesResource context)
    {
        if (context.Commands.Count == 0)
        {
            return container;
        }

        foreach (var command in context.Commands)
        {
            container.Args.Add(command);
        }

        return container;
    }

    private static ContainerV1 WithContainerEnvironmentalVariables(this ContainerV1 container, KubernetesResource context)
    {
        if (context.EnvironmentVariables.Count > 0)
        {
            container.EnvFrom.Add(
                new()
                {
                    ConfigMapRef = new()
                    {
                        Name = context.TargetResource.Name.ToConfigMapName(),
                    },
                });
        }

        return container;
    }

    private static ContainerV1 WithContainerSecrets(this ContainerV1 container, KubernetesResource context)
    {
        if (context.Secrets.Count > 0)
        {
            container.EnvFrom.Add(
                new()
                {
                    SecretRef = new()
                    {
                        Name = context.TargetResource.Name.ToSecretName(),
                    },
                });
        }

        return container;
    }

    private static PersistentVolume CreatePersistentVolume(KubernetesResource context, VolumeMountV1 volume)
    {
        var pvName = context.TargetResource.Name.ToPvName(volume.Name);

        if (context.PersistentVolumes.FirstOrDefault(pv => pv.Metadata.Name == pvName) is { } existingVolume)
        {
            return existingVolume;
        }

        var newPv = new PersistentVolume
        {
            Metadata =
            {
                Name = pvName,
                Labels = context.Labels.ToDictionary(),
            },
            Spec = new()
            {
                Capacity = new()
                {
                    ["storage"] = context.Parent.DefaultStorageSize,
                },
                AccessModes = { context.Parent.DefaultStorageReadWritePolicy },
            },
        };

        if (!string.IsNullOrEmpty(context.Parent.DefaultStorageClassName))
        {
            newPv.Spec.StorageClassName = context.Parent.DefaultStorageClassName;
        }

        if (context.Parent.DefaultStorageType.Equals("hostpath", StringComparison.OrdinalIgnoreCase))
        {
            newPv.Spec.HostPath = new()
            {
                Path = volume.Name,
            };
        }

        context.PersistentVolumes.Add(newPv);

        return newPv;
    }

    private static PersistentVolumeClaim CreatePersistentVolumeClaim(KubernetesResource context, VolumeMountV1 volume)
    {
        var pvcName = context.TargetResource.Name.ToPvcName(volume.Name);

        if (context.PersistentVolumeClaims.FirstOrDefault(pvc => pvc.Metadata.Name == pvcName) is { } existingVolumeClaim)
        {
            return existingVolumeClaim;
        }

        var pvc = new PersistentVolumeClaim
        {
            Metadata =
            {
                Name = pvcName,
                Labels = context.Labels.ToDictionary(),
            },
            Spec = new()
            {
                Resources = new(),
            },
        };

        pvc.Spec.AccessModes.Add(context.Parent.DefaultStorageReadWritePolicy);
        pvc.Spec.Resources.Requests.Add("storage", context.Parent.DefaultStorageSize);

        if (!string.IsNullOrEmpty(context.Parent.DefaultStorageClassName))
        {
            pvc.Spec.StorageClassName = context.Parent.DefaultStorageClassName;
        }

        context.PersistentVolumeClaims.Add(pvc);

        return pvc;
    }
}
