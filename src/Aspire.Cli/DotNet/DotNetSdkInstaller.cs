// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Aspire.Cli.Configuration;
using Microsoft.Extensions.Configuration;
using Semver;

namespace Aspire.Cli.DotNet;

/// <summary>
/// Default implementation of <see cref="IDotNetSdkInstaller"/> that checks for dotnet on the system PATH.
/// </summary>
internal sealed class DotNetSdkInstaller(IFeatures features, IConfiguration configuration) : IDotNetSdkInstaller
{
    /// <summary>
    /// The minimum .NET SDK version required for Aspire.
    /// </summary>
    public const string MinimumSdkVersion = "9.0.302";

    /// <summary>
    /// The minimum .NET SDK version required for Aspire when single-file apphost is enabled.
    /// </summary>
    public const string MinimumSdkVersionSingleFileAppHost = "10.0.100";

    /// <inheritdoc />
    public async Task<(bool Success, string? HighestVersion, string MinimumRequiredVersion)> CheckAsync(CancellationToken cancellationToken = default)
    {
        var minimumVersion = GetEffectiveMinimumSdkVersion();
        
        if (!features.IsFeatureEnabled(KnownFeatures.MinimumSdkCheckEnabled, true))
        {
            // If the feature is disabled, we assume the SDK is available
            return (true, null, minimumVersion);
        }

        try
        {
            // Add --arch flag to ensure we only get SDKs that match the current architecture
            var currentArch = GetCurrentArchitecture();
            var arguments = $"--list-sdks --arch {currentArch}";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return (false, null, minimumVersion);
            }

            // Parse the minimum version requirement
            if (!SemVersion.TryParse(minimumVersion, SemVersionStyles.Strict, out var minVersion))
            {
                return (false, null, minimumVersion);
            }

            // Parse each line of the output to find SDK versions
            var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            SemVersion? highestVersion = null;
            bool meetsMinimum = false;

            foreach (var line in lines)
            {
                // Each line is in format: "version [path]"
                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var versionString = line[..spaceIndex];
                    if (SemVersion.TryParse(versionString, SemVersionStyles.Strict, out var sdkVersion))
                    {
                        // Track the highest version
                        if (highestVersion == null || SemVersion.ComparePrecedence(sdkVersion, highestVersion) > 0)
                        {
                            highestVersion = sdkVersion;
                        }

                        // Check if this version meets the minimum requirement
                        if (SemVersion.ComparePrecedence(sdkVersion, minVersion) >= 0)
                        {
                            meetsMinimum = true;
                        }
                    }
                }
            }

            return (meetsMinimum, highestVersion?.ToString(), minimumVersion);
        }
        catch
        {
            // If we can't start the process, the SDK is not available
            return (false, null, minimumVersion);
        }
    }

    /// <inheritdoc />
    public Task InstallAsync(CancellationToken cancellationToken = default)
    {
        // Reserved for future implementation
        throw new NotImplementedException("SDK installation is not yet implemented.");
    }

    /// <summary>
    /// Gets the current architecture string in the format expected by dotnet --list-sdks --arch.
    /// </summary>
    /// <returns>The architecture string (e.g., "x64", "arm64", "x86", "arm").</returns>
    private static string GetCurrentArchitecture()
    {
        return RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64" // Default to x64 for unknown architectures
        };
    }

    /// <summary>
    /// Gets the effective minimum SDK version based on configuration and feature flags.
    /// </summary>
    /// <returns>The minimum SDK version string.</returns>
    public string GetEffectiveMinimumSdkVersion()
    {
        // Check for configuration override first
        var overrideVersion = configuration["overrideMinimumSdkVersion"];
        
        if (!string.IsNullOrEmpty(overrideVersion))
        {
            return overrideVersion;
        }
        else if (features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
        {
            return MinimumSdkVersionSingleFileAppHost;
        }
        else
        {
            return MinimumSdkVersion;
        }
    }
}