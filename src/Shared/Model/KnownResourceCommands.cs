// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

internal static class KnownResourceCommands
{
    public const string StartCommand = "resource-start";
    public const string StopCommand = "resource-stop";
    public const string RestartCommand = "resource-restart";

    public static bool IsKnownCommand(string command)
    {
        return command is StartCommand or StopCommand or RestartCommand;
    }
}
