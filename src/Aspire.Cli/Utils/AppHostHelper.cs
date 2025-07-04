// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Semver;
using System.Diagnostics;
using System.Globalization;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

internal static class AppHostHelper
{

    internal static async Task<(bool IsCompatibleAppHost, bool SupportsBackchannel, string? AspireHostingVersion)> CheckAppHostCompatibilityAsync(IDotNetCliRunner runner, IConsoleService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, CancellationToken cancellationToken)
    {
            var appHostInformation = await GetAppHostInformationAsync(runner, interactionService, projectFile, telemetry, cancellationToken);

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

            var compatibleRanges = SemVersionRange.Parse("^9.2.0-dev", SemVersionRangeOptions.IncludeAllPrerelease);
            if (!aspireVersion.Satisfies(compatibleRanges))
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

    internal static async Task<(int ExitCode, bool IsAspireHost, string? AspireHostingVersion)> GetAppHostInformationAsync(IDotNetCliRunner runner, IConsoleService interactionService, FileInfo projectFile, AspireCliTelemetry telemetry, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity(nameof(GetAppHostInformationAsync), ActivityKind.Client);

        var appHostInformationResult = await interactionService.ShowStatusAsync(
            $":microscope: {ConsoleServiceStrings.CheckingProjectType}",
            () => runner.GetAppHostInformationAsync(
                projectFile,
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken));

        return appHostInformationResult;
    }

    internal static async Task<int> BuildAppHostAsync(IDotNetCliRunner runner, IConsoleService interactionService, FileInfo projectFile, DotNetCliRunnerInvocationOptions options, CancellationToken cancellationToken)
    {
        return await interactionService.ShowStatusAsync(
            $":hammer_and_wrench:  {ConsoleServiceStrings.BuildingAppHost}",
            () => runner.BuildAsync(
                projectFile,
                options,
                cancellationToken));
    }
}
