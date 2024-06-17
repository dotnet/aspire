// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.Extensions;

public static class ResourceViewModelExtensions
{
    internal static bool IsHiddenState(this ResourceViewModel resource)
    {
        return resource.KnownState == KnownResourceState.Hidden;
    }

    internal static bool IsRunningState(this ResourceViewModel resource)
    {
        return resource.KnownState == KnownResourceState.Running;
    }

    internal static bool IsFinishedState(this ResourceViewModel resource)
    {
        return resource.KnownState is KnownResourceState.Finished;
    }

    internal static bool IsStopped(this ResourceViewModel resource)
    {
        return resource.KnownState is KnownResourceState.Exited or KnownResourceState.Finished or KnownResourceState.FailedToStart;
    }

    internal static bool IsStartingOrBuilding(this ResourceViewModel resource)
    {
        return resource.KnownState is KnownResourceState.Starting or KnownResourceState.Building;
    }

    internal static bool HasNoState(this ResourceViewModel resource) => string.IsNullOrEmpty(resource.State);
}
