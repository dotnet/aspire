// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Semver;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

internal static class AppHostHelper
{
    internal static async Task<(bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)> CheckAppHostCompatibilityAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var appHostInformation = await GetAppHostInformationAsync(runner, interactionService, projectFile, telemetry, workingDirectory, cancellationToken);

        if (appHostInformation.ExitCode != 0)
        {
            interactionService.DisplayError(ErrorStrings.ProjectCouldNotBeAnalyzed);
            return (false, false, null);
        }

        if (!appHostInformation.IsAspireHost)
        {
            interactionService.DisplayError(ErrorStrings.ProjectIsNotAppHost);
            return (false, false, null);
        }

        if (!SemVersion.TryParse(appHostInformation.AspireHostingVersion, out var aspireVersion))
        {
            interactionService.DisplayError(ErrorStrings.CouldNotParseAspireSDKVersion);
            return (false, false, null);
        }

        var minimumVersion = SemVersion.Parse("9.2.0");
        if (aspireVersion.ComparePrecedenceTo(minimumVersion) < 0)
        {
            interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ErrorStrings.AspireSDKVersionNotSupported, appHostInformation.AspireHostingVersion));
            return (false, false, appHostInformation.AspireHostingVersion);
        }
        else
        {
            // NOTE: When we go to support < 9.2.0 app hosts this is where we'll make
            //       a determination as to whether the apphsot supports backchannel or not.
            return (true, true, appHostInformation.AspireHostingVersion);
        }
    }

    internal static async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity(nameof(GetAppHostInformationAsync), ActivityKind.Client);

        var relativePath = Path.GetRelativePath(workingDirectory.FullName, projectFile.FullName);
        var appHostInformationResult = await interactionService.ShowStatusAsync(
            $":microscope: {InteractionServiceStrings.CheckingProjectType}: {relativePath}",
            () => runner.GetAppHostInformationAsync(
                projectFile,
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken));

        return appHostInformationResult;
    }

    internal static async Task<int> BuildAppHostAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, DirectoryInfo workingDirectory, CancellationToken cancellationToken)
    {
        var relativePath = Path.GetRelativePath(workingDirectory.FullName, projectFile.FullName);
        return await interactionService.ShowStatusAsync(
            $":hammer_and_wrench:  {InteractionServiceStrings.BuildingAppHost} {relativePath}",
            () => runner.BuildAsync(
                projectFile,
                options,
                cancellationToken));
    }

    /// <summary>
    /// Computes the auxiliary backchannel socket path for a given AppHost project file.
    /// This uses the same logic as AuxiliaryBackchannelService to ensure consistency.
    /// </summary>
    /// <param name="appHostPath">The full path to the AppHost project file or assembly.</param>
    /// <param name="homeDirectory">The user's home directory.</param>
    /// <returns>The computed socket path.</returns>
    internal static string ComputeAuxiliarySocketPath(string appHostPath, string homeDirectory)
    {
        const int HashLength = 16; // Use 16 characters to keep Unix socket path length reasonable
        
        var backchannelsDir = Path.Combine(homeDirectory, ".aspire", "cli", "backchannels");
        
        // Compute hash from the AppHost path for consistency
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(appHostPath));
        // Use limited characters to keep socket path length reasonable (Unix socket path limits)
        var hash = Convert.ToHexString(hashBytes)[..HashLength].ToLowerInvariant();
        
        var socketPath = Path.Combine(backchannelsDir, $"aux.sock.{hash}");
        return socketPath;
    }
}
