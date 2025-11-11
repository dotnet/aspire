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
        // Get version by parsing the SDK version string
        Version? parsedVersion = null;

        try
        {
            var (task, _) = ProcessUtil.Run(new("dotnet")
            {
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                Arguments = "--version",
                EnvironmentVariables = s_dotnetCliEnvVars,
                OnOutputData = data =>
                {
                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        // The SDK version is in the first line of output
                        var line = data.AsSpan().Trim();
                        // Trim any pre-release suffix
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
                return parsedVersion;
            }
        }
        catch (Exception) { }
        return null;
    }
}
