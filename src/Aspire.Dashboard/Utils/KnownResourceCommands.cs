// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard;

/// <summary>
/// Provides constants for well-known resource command names.
/// </summary>
internal static class KnownResourceCommands
{
    /// <summary>
    /// The command name for starting a resource.
    /// </summary>
    public const string StartCommand = "resource-start";

    /// <summary>
    /// The command name for stopping a resource.
    /// </summary>
    public const string StopCommand = "resource-stop";

    /// <summary>
    /// The command name for restarting a resource.
    /// </summary>
    public const string RestartCommand = "resource-restart";

    /// <summary>
    /// Determines whether the specified command is a known resource command.
    /// </summary>
    /// <param name="command">The command name to check.</param>
    /// <returns><see langword="true"/> if the command is a known resource command; otherwise, <see langword="false"/>.</returns>
    public static bool IsKnownCommand(string command)
    {
        return command == StartCommand || command == StopCommand || command == RestartCommand;
    }
}