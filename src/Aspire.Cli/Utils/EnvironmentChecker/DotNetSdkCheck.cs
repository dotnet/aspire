// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if the .NET SDK is installed and meets the minimum version requirement.
/// </summary>
internal sealed class DotNetSdkCheck(IDotNetSdkInstaller sdkInstaller, ILogger<DotNetSdkCheck> logger) : IEnvironmentCheck
{
    public int Order => 30; // File system check - slightly more expensive

    public async Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var (success, highestVersion, minimumRequiredVersion, _) = await sdkInstaller.CheckAsync(cancellationToken);

            if (!success)
            {
                // Parse major version from string like "10.0.100" -> 10
                var majorVersion = 10;
                if (Version.TryParse(minimumRequiredVersion, out var parsedVersion))
                {
                    majorVersion = parsedVersion.Major;
                }

                return [new EnvironmentCheckResult
                {
                    Category = "sdk",
                    Name = "dotnet-sdk",
                    Status = EnvironmentCheckStatus.Fail,
                    Message = highestVersion is null
                        ? ".NET SDK not found"
                        : $".NET {highestVersion} found but {minimumRequiredVersion} or higher required",
                    Fix = $"Download .NET SDK from: https://dotnet.microsoft.com/download/dotnet/{majorVersion}.0",
                    Link = $"https://dotnet.microsoft.com/download/dotnet/{majorVersion}.0"
                }];
            }

            var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

            return [new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dotnet-sdk",
                Status = EnvironmentCheckStatus.Pass,
                Message = $".NET {highestVersion} installed ({architecture})"
            }];
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error checking .NET SDK");
            return [new EnvironmentCheckResult
            {
                Category = "sdk",
                Name = "dotnet-sdk",
                Status = EnvironmentCheckStatus.Fail,
                Message = "Error checking .NET SDK",
                Details = ex.Message
            }];
        }
    }
}
