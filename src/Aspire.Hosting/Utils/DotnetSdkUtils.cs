// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using Aspire.Hosting.Execution;

namespace Aspire.Hosting.Utils;

internal interface IDotnetSdkService
{
    Task<Version?> TryGetVersionAsync(string? workingDirectory);
}

internal sealed class DotnetSdkService : IDotnetSdkService
{
    private static readonly Dictionary<string, string?> s_dotnetCliEnvVars = new()
    {
        ["DOTNET_NOLOGO"] = "true",
        ["DOTNET_GENERATE_ASPNET_CERTIFICATE"] = "false",
        ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true",
        ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true",
        ["SuppressNETCoreSdkPreviewMessage"] = "true"
    };

    private readonly IVirtualShell _shell;

    public DotnetSdkService(IVirtualShell shell)
    {
        _shell = shell;
    }

    public async Task<Version?> TryGetVersionAsync(string? workingDirectory)
    {
        try
        {
            var result = await _shell
                .Cd(workingDirectory ?? Environment.CurrentDirectory)
                .Env(s_dotnetCliEnvVars)
                .Command("dotnet", ["--version"])
                .RunAsync()
                .ConfigureAwait(false);

            if (!result.Success)
            {
                return null;
            }

            var output = result.Stdout;

            if (!string.IsNullOrWhiteSpace(output))
            {
                // The SDK version is in the output
                var line = output.AsSpan().Trim();
                // Trim any pre-release suffix
                var hyphenIndex = line.IndexOf('-');
                var versionSpan = hyphenIndex >= 0 ? line[..hyphenIndex] : line;
                if (Version.TryParse(versionSpan, out var version))
                {
                    return version;
                }
            }
        }
        catch (Exception)
        {
            // Best effort - return null on any error
        }

        return null;
    }
}
