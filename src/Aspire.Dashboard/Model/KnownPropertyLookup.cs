// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static Aspire.Dashboard.Resources.Resources;

namespace Aspire.Dashboard.Model;

public interface IKnownPropertyLookup
{
    (int priority, KnownProperty? knownProperty) FindProperty(string resourceType, string uid);
}

public sealed class KnownPropertyLookup : IKnownPropertyLookup
{
    private readonly List<KnownProperty> _resourceProperties;
    private readonly List<KnownProperty> _projectProperties;
    private readonly List<KnownProperty> _executableProperties;
    private readonly List<KnownProperty> _containerProperties;

    public KnownPropertyLookup()
    {
        _resourceProperties =
        [
            new(KnownProperties.Resource.DisplayName, loc => loc[nameof(ResourcesDetailsDisplayNameProperty)]),
            new(KnownProperties.Resource.State, loc => loc[nameof(ResourcesDetailsStateProperty)]),
            new(KnownProperties.Resource.StartTime, loc => loc[nameof(ResourcesDetailsStartTimeProperty)]),
            new(KnownProperties.Resource.StopTime, loc => loc[nameof(ResourcesDetailsStopTimeProperty)]),
            new(KnownProperties.Resource.ExitCode, loc => loc[nameof(ResourcesDetailsExitCodeProperty)]),
            new(KnownProperties.Resource.HealthState, loc => loc[nameof(ResourcesDetailsHealthStateProperty)]),
            new(KnownProperties.Resource.ConnectionString, loc => loc[nameof(ResourcesDetailsConnectionStringProperty)])
        ];

        _projectProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Project.Path, loc => loc[nameof(ResourcesDetailsProjectPathProperty)]),
            new(KnownProperties.Executable.Pid, loc => loc[nameof(ResourcesDetailsExecutableProcessIdProperty)]),
        ];

        _executableProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Executable.Path, loc => loc[nameof(ResourcesDetailsExecutablePathProperty)]),
            new(KnownProperties.Executable.WorkDir, loc => loc[nameof(ResourcesDetailsExecutableWorkingDirectoryProperty)]),
            new(KnownProperties.Executable.Args, loc => loc[nameof(ResourcesDetailsExecutableArgumentsProperty)]),
            new(KnownProperties.Executable.Pid, loc => loc[nameof(ResourcesDetailsExecutableProcessIdProperty)]),
        ];

        _containerProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Container.Image, loc => loc[nameof(ResourcesDetailsContainerImageProperty)]),
            new(KnownProperties.Container.Id, loc => loc[nameof(ResourcesDetailsContainerIdProperty)]),
            new(KnownProperties.Container.Command, loc => loc[nameof(ResourcesDetailsContainerCommandProperty)]),
            new(KnownProperties.Container.Args, loc => loc[nameof(ResourcesDetailsContainerArgumentsProperty)]),
            new(KnownProperties.Container.Ports, loc => loc[nameof(ResourcesDetailsContainerPortsProperty)]),
            new(KnownProperties.Container.Lifetime, loc => loc[nameof(ResourcesDetailsContainerLifetimeProperty)]),
        ];
    }

    public (int priority, KnownProperty? knownProperty) FindProperty(string resourceType, string uid)
    {
        var knownProperties = resourceType switch
        {
            KnownResourceTypes.Project => _projectProperties,
            KnownResourceTypes.Executable => _executableProperties,
            KnownResourceTypes.Container => _containerProperties,
            _ => _resourceProperties
        };

        for (var i = 0; i < knownProperties.Count; i++)
        {
            var kp = knownProperties[i];
            if (kp.Key == uid)
            {
                return (i, kp);
            }
        }

        return (int.MaxValue, null);
    }
}
