// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui;

/// <summary>
/// Configuration for a specific MAUI platform (Windows, Android, iOS, MacCatalyst).
/// </summary>
internal sealed class MauiPlatformConfiguration
{
    /// <summary>
    /// The platform moniker (e.g., "windows", "android", "ios", "maccatalyst").
    /// </summary>
    public required string Moniker { get; init; }

    /// <summary>
    /// The FluentUI icon name to display in the dashboard for this platform.
    /// </summary>
    public required string IconName { get; init; }

    /// <summary>
    /// Determines if this platform is supported on the current host OS.
    /// </summary>
    public required Func<bool> IsSupportedOnCurrentHost { get; init; }

    /// <summary>
    /// Human-readable reason when the platform is not supported on the current host.
    /// </summary>
    public required string UnsupportedReason { get; init; }

    /// <summary>
    /// Well-known platform configurations for all MAUI platforms.
    /// </summary>
    public static class KnownPlatforms
    {
        public static readonly MauiPlatformConfiguration Windows = new()
        {
            Moniker = "windows",
            IconName = "Desktop",
            IsSupportedOnCurrentHost = OperatingSystem.IsWindows,
            UnsupportedReason = "Windows platform requires running on a Windows host."
        };

        public static readonly MauiPlatformConfiguration Android = new()
        {
            Moniker = "android",
            IconName = "PhoneTablet",
            IsSupportedOnCurrentHost = () => true, // Android build tools can run on both Windows and macOS
            UnsupportedReason = "Android platform is not supported on this host."
        };

        public static readonly MauiPlatformConfiguration iOS = new()
        {
            Moniker = "ios",
            IconName = "PhoneTablet",
            IsSupportedOnCurrentHost = OperatingSystem.IsMacOS,
            UnsupportedReason = "iOS platform requires running on a macOS host with appropriate tooling."
        };

        public static readonly MauiPlatformConfiguration MacCatalyst = new()
        {
            Moniker = "maccatalyst",
            IconName = "DesktopMac",
            IsSupportedOnCurrentHost = OperatingSystem.IsMacOS,
            UnsupportedReason = "MacCatalyst platform requires running on a macOS host."
        };

        /// <summary>
        /// Gets the platform configuration for the specified moniker.
        /// </summary>
        public static MauiPlatformConfiguration? GetByMoniker(string moniker) => moniker.ToLowerInvariant() switch
        {
            "windows" => Windows,
            "android" => Android,
            "ios" => iOS,
            "maccatalyst" => MacCatalyst,
            _ => null
        };
    }
}
