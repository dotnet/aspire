// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.TestUtilities;

internal static class VerifyExtensions
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Set the directory for all Verify calls in test projects

        var target = PlatformDetection.IsRunningOnHelix
            ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD")!, "Snapshots")
            : "Snapshots";

        // If target contains an absolute path it will use it as is.
        // If it contains a relative path, it will be combined with the project directory.
        DerivePathInfo(
            (sourceFile, projectDirectory, type, method) => new(
                directory: Path.Combine(projectDirectory, target),
                typeName: type.Name,
                methodName: method.Name));
    }

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
