// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Processes;

internal static partial class DetachedProcessLauncher
{
    /// <summary>
    /// Unix implementation using Process.Start with stdio redirection.
    /// On Linux/macOS, .NET creates pipes with O_CLOEXEC so grandchild processes
    /// never inherit them across execve(). We just close the parent-side pipe
    /// streams immediately after start to suppress child output.
    /// </summary>
    private static Process StartUnix(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            WorkingDirectory = workingDirectory
        };

        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start detached process");

        // Close the parent's read-end of the pipes. The child's write-end has
        // O_CLOEXEC set by .NET, so when the child calls execve() to launch the
        // AppHost grandchild, the pipe fds are automatically closed.
        process.StandardOutput.Close();
        process.StandardError.Close();

        return process;
    }
}
