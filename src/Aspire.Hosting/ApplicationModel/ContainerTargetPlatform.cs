// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies the target platform for container images.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
[Flags]
public enum ContainerTargetPlatform
{
    /// <summary>
    /// Linux AMD64 (linux/amd64).
    /// </summary>
    LinuxAmd64 = 1,

    /// <summary>
    /// Linux ARM64 (linux/arm64).
    /// </summary>
    LinuxArm64 = 2,

    /// <summary>
    /// Linux ARM (linux/arm).
    /// </summary>
    LinuxArm = 4,

    /// <summary>
    /// Linux 386 (linux/386).
    /// </summary>
    Linux386 = 8,

    /// <summary>
    /// Windows AMD64 (windows/amd64).
    /// </summary>
    WindowsAmd64 = 16,

    /// <summary>
    /// Windows ARM64 (windows/arm64).
    /// </summary>
    WindowsArm64 = 32,

    /// <summary>
    /// All Linux platforms (AMD64 and ARM64).
    /// </summary>
    AllLinux = LinuxAmd64 | LinuxArm64
}

/// <summary>
/// Extension methods for <see cref="ContainerTargetPlatform"/>.
/// </summary>
[Experimental("ASPIREPIPELINES003", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
internal static class ContainerTargetPlatformExtensions
{
    /// <summary>
    /// Converts the target platform to the format used by container runtimes (Docker/Podman).
    /// </summary>
    /// <param name="platform">The target platform.</param>
    /// <returns>The platform string in the format used by container runtimes.</returns>
    public static string ToRuntimePlatformString(this ContainerTargetPlatform platform)
    {
        var platforms = new List<string>();

        if (platform.HasFlag(ContainerTargetPlatform.LinuxAmd64))
        {
            platforms.Add("linux/amd64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm64))
        {
            platforms.Add("linux/arm64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm))
        {
            platforms.Add("linux/arm");
        }
        if (platform.HasFlag(ContainerTargetPlatform.Linux386))
        {
            platforms.Add("linux/386");
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsAmd64))
        {
            platforms.Add("windows/amd64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsArm64))
        {
            platforms.Add("windows/arm64");
        }

        if (platforms.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unknown container target platform");
        }

        return string.Join(",", platforms);
    }

    /// <summary>
    /// Converts the target platform to the format used by MSBuild RuntimeIdentifiers.
    /// </summary>
    /// <param name="platform">The target platform.</param>
    /// <returns>The platform string in the format used by MSBuild.</returns>
    public static string ToMSBuildRuntimeIdentifierString(this ContainerTargetPlatform platform)
    {
        var rids = new List<string>();

        if (platform.HasFlag(ContainerTargetPlatform.LinuxAmd64))
        {
            rids.Add("linux-x64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm64))
        {
            rids.Add("linux-arm64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.LinuxArm))
        {
            rids.Add("linux-arm");
        }
        if (platform.HasFlag(ContainerTargetPlatform.Linux386))
        {
            rids.Add("linux-x86");
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsAmd64))
        {
            rids.Add("win-x64");
        }
        if (platform.HasFlag(ContainerTargetPlatform.WindowsArm64))
        {
            rids.Add("win-arm64");
        }

        if (rids.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(platform), platform, "Unknown container target platform");
        }

        return string.Join(";", rids);
    }
}
