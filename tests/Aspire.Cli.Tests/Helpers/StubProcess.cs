// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Tests.Helpers;

public class StubProcess(Process process) : IDisposable
{
    public Process Process { get; } = process;

    public static StubProcess Create()
    {
        var startInfo = Environment.OSVersion.Platform switch {
            PlatformID.Win32NT => new ProcessStartInfo()
            {
                FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                Arguments = "/c set %never%=WaitUntilDeath"
            },
            PlatformID.Unix => new ProcessStartInfo()
            {
                FileName = "/usr/bin/sleep",
                Arguments = "infinity"
            },
            _ => throw new PlatformNotSupportedException()
        };

        if (Process.Start(startInfo) is not {} process)
        {
            throw new InvalidOperationException("Failed to start stub process");
        }

        return new StubProcess(process);
    }

    public void Dispose()
    {
        Process.Dispose();
    }
}