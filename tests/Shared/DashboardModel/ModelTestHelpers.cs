// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Tests.Shared.DashboardModel;

public static class ModelTestHelpers
{
    public static ResourceViewModel CreateResource(
        string? appName = null,
        KnownResourceState? state = null,
        string? displayName = null,
        ImmutableArray<UrlViewModel>? urls = null,
        Dictionary<string, ResourcePropertyViewModel>? properties = null,
        string? resourceType = null)
    {
        return new ResourceViewModel
        {
            Name = appName ?? "Name!",
            ResourceType = resourceType ?? KnownResourceTypes.Container,
            DisplayName = displayName ?? appName ?? "Display name!",
            Uid = Guid.NewGuid().ToString(),
            CreationTimeStamp = DateTime.UtcNow,
            StartTimeStamp = DateTime.UtcNow,
            StopTimeStamp = DateTime.UtcNow,
            Environment = [],
            Urls = urls ?? [],
            Volumes = [],
            Properties = properties?.ToFrozenDictionary() ?? FrozenDictionary<string, ResourcePropertyViewModel>.Empty,
            State = state?.ToString(),
            KnownState = state,
            StateStyle = null,
            HealthStatus = HealthStatus.Healthy,
            HealthReports = [],
            Commands = [],
            Relationships = [],
        };
    }
}
