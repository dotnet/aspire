// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Graph;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Creates <see cref="IRuntimeProcessLauncher"/> for a given root project.
/// This gives dotnet-watch the ability to watch for and apply changes to
/// child processes that the root project application launches.
/// </summary>
internal interface IRuntimeProcessLauncherFactory
{
    public IRuntimeProcessLauncher? TryCreate(ProjectGraphNode projectNode, ProjectLauncher projectLauncher, ProjectOptions hostProjectOptions);
}
