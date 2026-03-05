// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;
using Microsoft.DotNet.HotReload;

namespace Microsoft.DotNet.Watch;

internal sealed class HotReloadProjectUpdatesBuilder
{
    public List<HotReloadService.Update> ManagedCodeUpdates { get; } = [];
    public Dictionary<RunningProject, List<StaticWebAsset>> StaticAssetsToUpdate { get; } = [];
    public List<string> ProjectsToRebuild { get; } = [];
    public List<string> ProjectsToRedeploy { get; } = [];
    public List<RunningProject> ProjectsToRestart { get; } = [];
}
