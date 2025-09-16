// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides constants for well-known resource command names.
/// </summary>
public static class KnownResourceCommands
{
    // Keep in sync with CommandViewModel in Aspire.Dashboard.

    /// <summary>
    /// The command name for starting a resource.
    /// </summary>
    public static readonly string StartCommand = "resource-start";

    /// <summary>
    /// The command name for stopping a resource.
    /// </summary>
    public static readonly string StopCommand = "resource-stop";

    /// <summary>
    /// The command name for restarting a resource.
    /// </summary>
    public static readonly string RestartCommand = "resource-restart";
}
