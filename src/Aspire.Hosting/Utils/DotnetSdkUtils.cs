// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Process;

namespace Aspire.Hosting.Utils;

internal static class DotnetSdkUtils
{
    private static readonly Dictionary<string, string> s_dotnetCliEnvVars = new()
    {
        ["DOTNET_NOLOGO"] = "true",
        ["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",
        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true",
        ["SuppressNETCoreSdkPreviewMessage"] = "true"
    };

    public static async Task<Version?> TryGetVersionAsync(string? workingDirectory)
    {
        var (version, _) = await RunDotnetVersionAsync(workingDirectory).ConfigureAwait(false);
        return version;
    }

    /// <summary>
    /// Resolves the active .NET SDK directory path by running <c>dotnet --list-sdks</c> and <c>dotnet --version</c>.
    /// Returns the full path to the SDK directory (e.g., <c>/usr/local/share/dotnet/sdk/10.0.100</c>), or null on failure.
    /// </summary>
    public static async Task<string?> TryGetSdkDirectoryAsync(string? workingDirectory)
    {
        var (_, rawVersionString) = await RunDotnetVersionAsync(workingDirectory).ConfigureAwait(false);
        if (rawVersionString is null)
        {
            return null;
        }

        // Use dotnet --list-sdks to find the actual path for this SDK version.
        // This handles cases where the SDK is in a non-standard location (e.g., repo-local .dotnet/).
        // Output format: "10.0.102 [/path/to/sdk]"
        var sdkPath = await FindSdkPathFromListAsync(rawVersionString, workingDirectory).ConfigureAwait(false);
        if (sdkPath is not null)
        {
            return sdkPath;
        }

        // Fallback: try well-known dotnet root locations
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { } hostPath
            ? Path.GetDirectoryName(hostPath)
            : Environment.GetEnvironmentVariable("DOTNET_ROOT");

        if (string.IsNullOrEmpty(dotnetRoot))
        {
            var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
            dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", ".."));
        }

        var sdkDir = Path.Combine(dotnetRoot, "sdk", rawVersionString);
        return Directory.Exists(sdkDir) ? sdkDir : null;
    }

    private static async Task<string?> FindSdkPathFromListAsync(string version, string? workingDirectory)
    {
        var lines = new List<string>();
        try
        {
            var (task, _) = ProcessUtil.Run(new("dotnet")
            {
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                Arguments = "--list-sdks",
                EnvironmentVariables = s_dotnetCliEnvVars,
                OnOutputData = data =>
                {
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        lines.Add(data.Trim());
                    }
                }
            });
            var result = await task.ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                return null;
            }
        }
        catch (Exception)
        {
            return null;
        }

        // Parse lines like: "10.0.102 [/Users/davidfowler/.dotnet/sdk]"
        foreach (var line in lines)
        {
            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex <= 0)
            {
                continue;
            }

            var lineVersion = line[..spaceIndex];
            if (!string.Equals(lineVersion, version, StringComparison.Ordinal))
            {
                continue;
            }

            // Extract the path from brackets: "[/path/to/sdk]"
            var bracketStart = line.IndexOf('[', spaceIndex);
            var bracketEnd = line.IndexOf(']', bracketStart + 1);
            if (bracketStart >= 0 && bracketEnd > bracketStart)
            {
                var basePath = line[(bracketStart + 1)..bracketEnd];
                var fullPath = Path.Combine(basePath, version);
                if (Directory.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }

    private static async Task<(Version? Parsed, string? Raw)> RunDotnetVersionAsync(string? workingDirectory)
    {
        Version? parsedVersion = null;
        string? rawVersionString = null;

        try
        {
            var (task, _) = ProcessUtil.Run(new("dotnet")
            {
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                Arguments = "--version",
                EnvironmentVariables = s_dotnetCliEnvVars,
                OnOutputData = data =>
                {
                    if (!string.IsNullOrWhiteSpace(data) && rawVersionString is null)
                    {
                        rawVersionString = data.Trim();

                        // Parse the version, trimming any pre-release suffix
                        var line = rawVersionString.AsSpan();
                        var hyphenIndex = line.IndexOf('-');
                        var versionSpan = hyphenIndex >= 0 ? line[..hyphenIndex] : line;
                        if (Version.TryParse(versionSpan, out var v))
                        {
                            parsedVersion = v;
                        }
                    }
                }
            });
            var result = await task.ConfigureAwait(false);
            if (result.ExitCode == 0)
            {
                return (parsedVersion, rawVersionString);
            }
        }
        catch (Exception) { }
        return (null, null);
    }
}
