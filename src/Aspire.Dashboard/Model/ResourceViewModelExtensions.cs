// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Model;

internal static class ResourceViewModelExtensions
{
    public static bool IsContainer(this ResourceViewModel resource)
    {
        return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Container);
    }

    public static bool IsProject(this ResourceViewModel resource)
    {
        return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Project);
    }

    public static bool IsExecutable(this ResourceViewModel resource, bool allowSubtypes)
    {
        if (StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Executable))
        {
            return true;
        }

        if (allowSubtypes)
        {
            return StringComparers.ResourceType.Equals(resource.ResourceType, KnownResourceTypes.Project);
        }

        return false;
    }

    public static bool TryGetExitCode(this ResourceViewModel resource, out int exitCode)
    {
        return resource.TryGetCustomDataInt(KnownProperties.Resource.ExitCode, out exitCode);
    }

    public static bool TryGetContainerImage(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? containerImage)
    {
        return resource.TryGetCustomDataString(KnownProperties.Container.Image, out containerImage);
    }

    public static bool TryGetProjectPath(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? projectPath)
    {
        return resource.TryGetCustomDataString(KnownProperties.Project.Path, out projectPath);
    }

    public static bool TryGetExecutablePath(this ResourceViewModel resource, [NotNullWhen(returnValue: true)] out string? executablePath)
    {
        return resource.TryGetCustomDataString(KnownProperties.Executable.Path, out executablePath);
    }

    public static bool TryGetExecutableArguments(this ResourceViewModel resource, out ImmutableArray<string> arguments)
    {
        return resource.TryGetCustomDataStringArray(KnownProperties.Executable.Args, out arguments);
    }

    public static bool TryGetAppArgs(this ResourceViewModel resource, out ImmutableArray<string> arguments)
    {
        return resource.TryGetCustomDataStringArray(KnownProperties.Resource.AppArgs, out arguments);
    }

    public static bool TryGetAppArgsSensitivity(this ResourceViewModel resource, out ImmutableArray<bool> argParams)
    {
        return resource.TryGetCustomDataBoolArray(KnownProperties.Resource.AppArgsSensitivity, out argParams);
    }

    private static bool TryGetCustomDataString(this ResourceViewModel resource, string key, [NotNullWhen(returnValue: true)] out string? s)
    {
        if (resource.Properties.TryGetValue(key, out var property) && property.Value.TryConvertToString(out var valueString))
        {
            s = valueString;
            return true;
        }

        s = null;
        return false;
    }

    private static bool TryGetCustomDataStringArray(this ResourceViewModel resource, string key, out ImmutableArray<string> strings)
    {
        if (resource.Properties.TryGetValue(key, out var property) && property is { Value: { ListValue: not null } value })
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

    private static bool TryGetCustomDataBoolArray(this ResourceViewModel resource, string key, out ImmutableArray<bool> bools)
    {
        if (resource.Properties.TryGetValue(key, out var property) && property is { Value: { ListValue: not null } value })
        {
            var builder = ImmutableArray.CreateBuilder<bool>(value.ListValue.Values.Count);

            foreach (var element in value.ListValue.Values)
            {
                if (!element.HasNumberValue)
                {
                    bools = default;
                    return false;
                }

                builder.Add(Convert.ToBoolean(element.NumberValue));
            }

            bools = builder.MoveToImmutable();
            return true;
        }

        bools = default;
        return false;
    }

    private static bool TryGetCustomDataInt(this ResourceViewModel resource, string key, out int i)
    {
        if (resource.Properties.TryGetValue(key, out var property) && property.Value.TryConvertToInt(out i))
        {
            return true;
        }

        i = 0;
        return false;
    }
}
