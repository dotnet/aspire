// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Execution;

namespace Microsoft.DotNet.Watch;

internal readonly struct BuildResult<T>(IReadOnlyDictionary<string, TargetResult> targetResults, ProjectInstance projectInstance, T data)
{
    public IReadOnlyDictionary<string, TargetResult> TargetResults { get; } = targetResults;
    public ProjectInstance ProjectInstance { get; } = projectInstance;
    public T Data { get; } = data;

    public bool IsSuccess => TargetResults.Count > 0;
}
