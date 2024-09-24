// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;
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

    public KnownPropertyLookup(IStringLocalizer<Resources.Resources> loc)
    {
        _resourceProperties =
        [
            new(KnownProperties.Resource.DisplayName, loc[ResourcesDetailsDisplayNameProperty]),
            new(KnownProperties.Resource.State, loc[ResourcesDetailsStateProperty]),
            new(KnownProperties.Resource.StartTime, loc[ResourcesDetailsStartTimeProperty]),
            new(KnownProperties.Resource.StopTime, loc[ResourcesDetailsStopTimeProperty]),
            new(KnownProperties.Resource.ExitCode, loc[ResourcesDetailsExitCodeProperty]),
            new(KnownProperties.Resource.HealthState, loc[ResourcesDetailsHealthStateProperty])
        ];

        _projectProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Project.Path, loc[ResourcesDetailsProjectPathProperty]),
            new(KnownProperties.Executable.Pid, loc[ResourcesDetailsExecutableProcessIdProperty]),
        ];

        _executableProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Executable.Path, loc[ResourcesDetailsExecutablePathProperty]),
            new(KnownProperties.Executable.WorkDir, loc[ResourcesDetailsExecutableWorkingDirectoryProperty]),
            new(KnownProperties.Executable.Args, loc[ResourcesDetailsExecutableArgumentsProperty]),
            new(KnownProperties.Executable.Pid, loc[ResourcesDetailsExecutableProcessIdProperty]),
        ];

        _containerProperties =
        [
            .. _resourceProperties,
            new(KnownProperties.Container.Image, loc[ResourcesDetailsContainerImageProperty]),
            new(KnownProperties.Container.Id, loc[ResourcesDetailsContainerIdProperty]),
            new(KnownProperties.Container.Command, loc[ResourcesDetailsContainerCommandProperty]),
            new(KnownProperties.Container.Args, loc[ResourcesDetailsContainerArgumentsProperty]),
            new(KnownProperties.Container.Ports, loc[ResourcesDetailsContainerPortsProperty]),
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
