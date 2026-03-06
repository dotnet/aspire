// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Execution;

namespace Microsoft.DotNet.Watch;

internal readonly struct BuildRequest<T>(ProjectInstance projectInstance, ImmutableArray<string> targets, T data)
{
    public ProjectInstance ProjectInstance { get; } = projectInstance;
    public ImmutableArray<string> Targets { get; } = targets;
    public T Data { get; } = data;
}

internal static class BuildRequest
{
    public static BuildRequest<object?> Create(ProjectInstance instance, ImmutableArray<string> targets)
        => new(instance, targets, data: null);

    public static BuildRequest<T> Create<T>(ProjectInstance instance, ImmutableArray<string> targets, T data)
        => new(instance, targets, data);
}
