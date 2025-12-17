// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Wrapper around <see cref="ActivitySource"/> for VirtualShell tracing.
/// Enables dependency injection of the activity source.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class VirtualShellActivitySource : IDisposable
{
    private readonly ActivitySource _activitySource = new("Aspire.VirtualShell");

    /// <summary>
    /// Starts a new activity with the specified name.
    /// </summary>
    /// <param name="name">The activity name (typically the command/executable name).</param>
    /// <returns>The started activity, or null if no listeners are registered.</returns>
    public Activity? StartActivity(string name) => _activitySource.StartActivity(name);

    /// <inheritdoc />
    public void Dispose() => _activitySource.Dispose();
}
