// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Semver;
using System.Diagnostics;

namespace Aspire.Cli.Utils;

internal static class AppHostHelper
{
    private static readonly ActivitySource s_activitySource = new ActivitySource(nameof(AppHostHelper));

    internal static async Task<(bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)> CheckAppHostCompatibilityAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, CancellationToken cancellationToken)
    {
            var appHostInformation = await GetAppHostInformationAsync(runner, interactionService, projectFile, cancellationToken);

            if (appHostInformation.ExitCode != 0)
            {
                interactionService.DisplayError("The project could not be analyzed due to a build error. For more information run with --debug switch.");
                return (false, false, null);
            }

            if (!appHostInformation.IsAspireHost)
            {
                interactionService.DisplayError($"The project is not an Aspire app host project.");
                return (false, false, null);
            }

            if (!SemVersion.TryParse(appHostInformation.AspireHostingVersion, out var aspireVersion))
            {
                interactionService.DisplayError($"Could not parse Aspire.Hosting package version.");
                return (false, false, null);
            }

            var compatibleRanges = SemVersionRange.Parse("^9.2.0-dev", SemVersionRangeOptions.IncludeAllPrerelease);
            if (!aspireVersion.Satisfies(compatibleRanges))
            {
                interactionService.DisplayError($"The Aspire.Hosting package version '{appHostInformation.AspireHostingVersion}' is not supported. Please update to the latest version.");
                return (false, false, appHostInformation.AspireHostingVersion);
            }
            else
            {
                // NOTE: When we go to support < 9.2.0 app hosts this is where we'll make
                //       a determination as to whether the apphsot supports backchannel or not.
                return (true, true, appHostInformation.AspireHostingVersion);
            }
    }

    internal static async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, CancellationToken cancellationToken)
    {
        using var activity = s_activitySource.StartActivity(nameof(GetAppHostInformationAsync), ActivityKind.Client);

        var appHostInformationResult = await interactionService.ShowStatusAsync(
            ":microscope: Checking project type...",
            () => runner.GetAppHostInformationAsync(
                projectFile,
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken));

        return appHostInformationResult;
    }
    
    internal static async Task<int> BuildAppHostAsync(IDotNetCliRunner runner, IInteractionService interactionService, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return await interactionService.ShowStatusAsync(
            ":hammer_and_wrench:  Building app host...",
            () => runner.BuildAsync(
                projectFile,
                options,
                cancellationToken));
    }
}