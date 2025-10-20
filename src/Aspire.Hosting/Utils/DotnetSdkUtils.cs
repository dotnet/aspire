// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Utils;

internal static class DotnetSdkUtils
{
    public static bool TryGetVersion(string? workingDirectory, [NotNullWhen(true)] out Version? version)
    {
        var task = TryGetVersionAsync(workingDirectory);
        task.GetAwaiter().GetResult();
        version = task.Result;
        return version is not null;
    }

    public static async Task<Version?> TryGetVersionAsync(string? workingDirectory)
    {
        // Get version by parsing the SDK version string
        var startInfo = new ProcessStartInfo("dotnet", ["--version"])
        {
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.EnvironmentVariables.Add("DOTNET_NOLOGO", "true");
        startInfo.EnvironmentVariables.Add("DOTNET_GENERATE_ASPNET_CERTIFICATE", "false");
        startInfo.EnvironmentVariables.Add("DOTNET_CLI_TELEMETRY_OPTOUT", "true");
        startInfo.EnvironmentVariables.Add("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "true");
        startInfo.EnvironmentVariables.Add("SuppressNETCoreSdkPreviewMessage", "true");

        try
        {
            using var process = new Process { StartInfo = startInfo };

            Version? parsedVersion = null;
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    // The SDK version is in the first line of output
                    var line = e.Data.AsSpan().Trim();
                    // Trim any pre-release suffix
                    var hyphenIndex = line.IndexOf('-');
                    if (Version.TryParse(line[..hyphenIndex], out var v))
                    {
                        parsedVersion = v;
                    }
                }
            };

            if (!process.Start())
            {
                return null;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync().ConfigureAwait(false);

            return parsedVersion;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
