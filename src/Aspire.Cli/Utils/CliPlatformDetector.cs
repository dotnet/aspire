// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Aspire.Cli.Utils;

/// <summary>
/// Detects the current platform for CLI downloads.
/// </summary>
internal static class CliPlatformDetector
{
    public static (string os, string arch) DetectPlatform()
    {
        var os = DetectOperatingSystem();
        var arch = DetectArchitecture();
        return (os, arch);
    }

    private static string DetectOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Check if it's musl-based (Alpine, etc.)
            return IsMuslBased() ? "linux-musl" : "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx";
        }
        else
        {
            throw new PlatformNotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }
    }

    private static string DetectArchitecture()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        return arch switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {arch}")
        };
    }

    private static bool IsMuslBased()
    {
        try
        {
            var lddPath = "/usr/bin/ldd";
            if (File.Exists(lddPath))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = lddPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var process = Process.Start(psi);
                if (process is not null)
                {
                    var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (output.Contains("musl", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Fall back to regular linux
        }
        return false;
    }
}
