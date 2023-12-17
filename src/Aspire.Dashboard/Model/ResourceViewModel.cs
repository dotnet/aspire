// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Dashboard.Model;

public sealed class ResourceViewModel
{
    public required string Name { get; init; }
    public required string ResourceType { get; init; }
    public required string DisplayName { get; init; }
    public required string Uid { get; init; }
    public required string? State { get; init; }
    public required DateTime? CreationTimeStamp { get; init; }
    public required ImmutableArray<EnvironmentVariableViewModel> Environment { get; init; }
    public required ImmutableArray<EndpointSnapshot> Endpoints { get; init; }
    public required ImmutableArray<ResourceServiceViewModel> Services { get; init; }
    public required int? ExpectedEndpointsCount { get; init; }
    public required FrozenDictionary<string, Value> Properties { get; init; }

    internal bool MatchesFilter(string filter)
    {
        // TODO let ResourceType define the additional data values we include in searches
        return Name.Contains(filter, StringComparisons.UserTextSearch);
    }

    public static string GetResourceName(ResourceViewModel resource, IEnumerable<ResourceViewModel> allResources)
    {
        var count = 0;
        foreach (var item in allResources)
        {
            if (item.DisplayName == resource.DisplayName)
            {
                count++;
                if (count >= 2)
                {
                    return ResourceFormatter.GetName(resource.DisplayName, resource.Uid);
                }
            }
        }

        return resource.DisplayName;
    }
}

public sealed class ResourceServiceViewModel(string name, string? allocatedAddress, int? allocatedPort)
{
    public string Name { get; } = name;
    public string? AllocatedAddress { get; } = allocatedAddress;
    public int? AllocatedPort { get; } = allocatedPort;
    public string AddressAndPort { get; } = $"{allocatedAddress}:{allocatedPort}";
}

public sealed record EndpointSnapshot(string EndpointUrl, string ProxyUrl);

internal static class ResourceViewModelExtensions
{
    public static bool IsContainer(this ResourceViewModel resource)
    {
        return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Container);
    }

    public static bool IsProject(this ResourceViewModel resource)
    {
        return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Container);
    }

    public static bool IsExecutable(this ResourceViewModel resource, bool allowSubtypes)
    {
        if (StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Container))
        {
            return true;
        }

        if (allowSubtypes)
        {
            return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Project);
        }

        return false;
    }

    public static bool TryGetContainerId(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? containerId)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Container.Id, out containerId);
    }

    public static bool TryGetContainerImage(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? containerImage)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Container.Image, out containerImage);
    }

    public static bool TryGetContainerPorts(this ResourceViewModel resource, out ImmutableArray<int> containerImage)
    {
        return resource.TryGetCustomDataIntArray(ResourceDataKeys.Container.Ports, out containerImage);
    }

    public static bool TryGetContainerCommand(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? command)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Container.Command, out command);
    }

    public static bool TryGetContainerArgs(this ResourceViewModel resource, out ImmutableArray<string> args)
    {
        return resource.TryGetCustomDataStringArray(ResourceDataKeys.Container.Args, out args);
    }

    public static bool TryGetProcessId(this ResourceViewModel resource, out int processId)
    {
        return resource.TryGetCustomDataInt(ResourceDataKeys.Executable.Pid, out processId);
    }

    public static bool TryGetProjectPath(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? projectPath)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Project.Path, out projectPath);
    }

    public static bool TryGetExecutablePath(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? executablePath)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Executable.Path, out executablePath);
    }

    public static bool TryGetExecutableArguments(this ResourceViewModel resource, out ImmutableArray<string> arguments)
    {
        return resource.TryGetCustomDataStringArray(ResourceDataKeys.Executable.Args, out arguments);
    }

    public static bool TryGetWorkingDirectory(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? workingDirectory)
    {
        return resource.TryGetCustomDataString(ResourceDataKeys.Executable.WorkDir, out workingDirectory);
    }

    private static bool TryGetCustomDataString(this ResourceViewModel resource, string key, [NotNullWhen(returnValue: true)] out string? s)
    {
        if (resource.Properties.TryGetValue(key, out var value) && value.TryConvertToString(out var valueString))
        {
            s = valueString;
            return true;
        }

        s = null;
        return false;
    }

    private static bool TryGetCustomDataStringArray(this ResourceViewModel resource, string key, out ImmutableArray<string> strings)
    {
        if (resource.Properties.TryGetValue(key, out var value) && value.ListValue is not null)
        {
            var builder = ImmutableArray.CreateBuilder<string>(value.ListValue.Values.Count);

            foreach (var element in value.ListValue.Values)
            {
                if (!element.TryConvertToString(out var elementString))
                {
                    strings = default;
                    return false;
                }

                builder.Add(elementString);
            }

            strings = builder.MoveToImmutable();
            return true;
        }

        strings = default;
        return false;
    }

    private static bool TryGetCustomDataInt(this ResourceViewModel resource, string key, out int i)
    {
        if (resource.Properties.TryGetValue(key, out var value) && value.TryConvertToInt(out i))
        {
            return true;
        }

        i = 0;
        return false;
    }

    private static bool TryGetCustomDataIntArray(this ResourceViewModel resource, string key, out ImmutableArray<int> ints)
    {
        if (resource.Properties.TryGetValue(key, out var value) && value.ListValue is not null)
        {
            var builder = ImmutableArray.CreateBuilder<int>(value.ListValue.Values.Count);

            foreach (var element in value.ListValue.Values)
            {
                if (!element.TryConvertToInt(out var i))
                {
                    ints = default;
                    return false;
                }
                builder.Add(i);
            }

            ints = builder.MoveToImmutable();
            return true;
        }

        ints = default;
        return false;
    }
}

internal static class ValueExtensions
{
    public static bool TryConvertToInt(this Value value, out int i)
    {
        if (value.HasStringValue && int.TryParse(value.StringValue, CultureInfo.InvariantCulture, out i))
        {
            return true;
        }
        else if (value.HasNumberValue)
        {
            i = (int)Math.Round(value.NumberValue);
            return true;
        }

        i = 0;
        return false;
    }

    public static bool TryConvertToString(this Value value, [NotNullWhen(returnValue: true)] out string? s)
    {
        if (value.HasStringValue)
        {
            s = value.StringValue;
            return true;
        }

        s = null;
        return false;
    }
}
