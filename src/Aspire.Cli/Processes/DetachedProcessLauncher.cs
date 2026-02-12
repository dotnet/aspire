// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Processes;

// ============================================================================
// DetachedProcessLauncher — Platform-aware child process launcher for --detach
// ============================================================================
//
// When `aspire run --detach` is used, the CLI spawns a child CLI process which
// in turn spawns the AppHost (the "grandchild"). Two constraints must hold:
//
// 1. The child's stdout/stderr must NOT appear on the parent's console.
//    The parent renders its own summary UX (dashboard URL, PID, log path) and
//    if the child's output (spinners, "Press CTRL+C", etc.) bleeds through, it
//    corrupts the parent's terminal — and breaks E2E tests that pattern-match
//    on the parent's output.
//
// 2. No pipe or handle from the parent→child stdio redirection may leak into
//    the grandchild (AppHost). If it does, callers that wait for the CLI's
//    stdout to close (e.g. Node.js `execSync`, shell `$(...)` substitution)
//    will hang until the AppHost exits — which defeats the purpose of --detach.
//
// These two constraints conflict when using .NET's Process.Start:
//
//   • RedirectStandardOutput = true  → solves (1) but violates (2) on Windows,
//     because .NET calls CreateProcess with bInheritHandles=TRUE, and the pipe
//     write-handle is duplicated into the child. The child passes it to the
//     grandchild (AppHost), keeping the pipe alive.
//
//   • RedirectStandardOutput = false → solves (2) but violates (1), because
//     the child inherits the parent's console and writes directly to it.
//
// The solution is platform-specific:
//
// ┌─────────┬────────────────────────────────────────────────────────────────┐
// │ Windows │ P/Invoke CreateProcess with STARTUPINFOEX and an explicit     │
// │         │ PROC_THREAD_ATTRIBUTE_HANDLE_LIST. This lets us set           │
// │         │ bInheritHandles=TRUE (required to assign hStdOutput to NUL)   │
// │         │ while restricting inheritance to ONLY the NUL handle — so the │
// │         │ grandchild inherits nothing useful. Child stdout/stderr go to │
// │         │ the NUL device. This is the same approach used by Docker's    │
// │         │ Windows container runtime (microsoft/hcsshim).                │
// │         │                                                               │
// │ Linux / │ Process.Start with RedirectStandard{Output,Error} = true,     │
// │ macOS   │ then immediately close the pipe streams. On Unix, .NET        │
// │         │ creates pipes with O_CLOEXEC, so the grandchild never         │
// │         │ inherits them across execve() — unlike Windows, this is safe. │
// │         │ This is the same model used by runc (opencontainers/runc),    │
// │         │ which relies on O_CLOEXEC + close-on-exec to prevent fd leaks │
// │         │ into container init processes.                                 │
// └─────────┴────────────────────────────────────────────────────────────────┘
//

/// <summary>
/// Launches a child process with stdout/stderr suppressed and no handle/fd
/// inheritance to grandchild processes. Used by <c>aspire run --detach</c>.
/// </summary>
internal static partial class DetachedProcessLauncher
{
    /// <summary>
    /// Starts a detached child process with stdout/stderr going to the null device
    /// and no inheritable handles/fds leaking to grandchildren.
    /// </summary>
    /// <param name="fileName">The executable path (e.g. dotnet or the native CLI).</param>
    /// <param name="arguments">The command-line arguments for the child process.</param>
    /// <param name="workingDirectory">The working directory for the child process.</param>
    /// <returns>A <see cref="Process"/> object representing the launched child.</returns>
    public static Process Start(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            return StartWindows(fileName, arguments, workingDirectory);
        }

        return StartUnix(fileName, arguments, workingDirectory);
    }
}
