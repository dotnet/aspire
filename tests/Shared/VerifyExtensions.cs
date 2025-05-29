// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;

internal static class VerifyExtensions
{
    /// <summary>
    /// Sets the directory for the Verify call in a way it's also compatible with Helix.
    /// </summary>
    public static SettingsTask UseHelixAwareDirectory(this SettingsTask settings, string directory = "Snapshots")
    {
        settings.UseDirectory(PlatformDetection.IsRunningOnHelix
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")!, directory)
            : directory);
        return settings;
    }
}
