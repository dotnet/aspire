// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Cli.Utils;

/// <summary>
/// Utility for sending signals to processes on Unix systems.
/// </summary>
internal static partial class ProcessSignal
{
    /// <summary>
    /// Sends SIGINT to a process for graceful shutdown.
    /// On Windows, this is a no-op as Ctrl+C flows to child processes automatically.
    /// </summary>
    public static void SendInterrupt(int pid)
    {
        if (!OperatingSystem.IsWindows())
        {
            sys_kill(pid, sig: 2); // SIGINT
        }
    }

    [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static partial int sys_kill(int pid, int sig);
}
