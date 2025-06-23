// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class UpdateCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly AspireCliTelemetry _telemetry;

    public UpdateCommand(IDotNetCliRunner runner, IInteractionService interactionService, IProjectLocator projectLocator, AspireCliTelemetry telemetry)
        : base("update", UpdateCommandStrings.Description)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(telemetry);

        _runner = runner;
        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _telemetry = telemetry;

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = UpdateCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var outputCollector = new OutputCollector();

        try
        {
            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            // Get package references and properties from the project
            var (exitCode, jsonDocument) = await _interactionService.ShowStatusAsync(
                UpdateCommandStrings.CheckingForUpdates,
                () => _runner.GetProjectItemsAndPropertiesAsync(
                    effectiveAppHostProjectFile,
                    ["PackageReference"],
                    ["AspireHostingSDKVersion"],
                    new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = outputCollector.AppendOutput,
                        StandardErrorCallback = outputCollector.AppendError,
                    },
                    cancellationToken)
            );

            if (exitCode != 0 || jsonDocument is null)
            {
                _interactionService.DisplayLines(outputCollector.GetLines());
                _interactionService.DisplayError(UpdateCommandStrings.PackageUpdateFailed);
                return ExitCodeConstants.FailedToAddPackage;
            }

            // Find Aspire packages
            var aspirePackages = FindAspirePackages(jsonDocument);

            if (!aspirePackages.Any())
            {
                _interactionService.DisplayMessage("ℹ️", UpdateCommandStrings.NoAspirePackagesFound);
                return ExitCodeConstants.Success;
            }

            // Check for updates (stub implementation)
            var packagesToUpdate = await CheckForUpdatesAsync(aspirePackages, cancellationToken);

            if (!packagesToUpdate.Any())
            {
                _interactionService.DisplayMessage("ℹ️", UpdateCommandStrings.NoUpdatesAvailable);
                return ExitCodeConstants.Success;
            }

            // Display packages to update and get confirmation
            var shouldUpdate = await ConfirmUpdatesAsync(packagesToUpdate, cancellationToken);

            if (!shouldUpdate)
            {
                _interactionService.DisplayMessage("ℹ️", UpdateCommandStrings.UpdateCancelled);
                return ExitCodeConstants.Success;
            }

            // Update packages
            var updateResult = await UpdatePackagesAsync(effectiveAppHostProjectFile, packagesToUpdate, outputCollector, cancellationToken);

            if (updateResult != ExitCodeConstants.Success)
            {
                _interactionService.DisplayLines(outputCollector.GetLines());
                _interactionService.DisplayError(UpdateCommandStrings.PackageUpdateFailed);
                return updateResult;
            }

            _interactionService.DisplaySuccess(UpdateCommandStrings.PackagesUpdatedSuccessfully);
            return ExitCodeConstants.Success;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.ProjectFileDoesntExist, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionDoesntExist);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.MultipleProjectFilesFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedMultipleAppHostsFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (ProjectLocatorException ex) when (string.Equals(ex.Message, ErrorStrings.NoProjectFileFound, StringComparisons.CliInputOrOutput))
        {
            _interactionService.DisplayError(InteractionServiceStrings.ProjectOptionNotSpecifiedNoCsprojFound);
            return ExitCodeConstants.FailedToFindProject;
        }
        catch (OperationCanceledException)
        {
            _interactionService.DisplayCancellationMessage();
            return ExitCodeConstants.FailedToAddPackage;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayLines(outputCollector.GetLines());
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, UpdateCommandStrings.PackageUpdateFailed, ex.Message));
            return ExitCodeConstants.FailedToAddPackage;
        }
    }

    private static List<AspirePackageInfo> FindAspirePackages(JsonDocument jsonDocument)
    {
        var packages = new List<AspirePackageInfo>();

        if (!jsonDocument.RootElement.TryGetProperty("Items", out var items) ||
            !items.TryGetProperty("PackageReference", out var packageReferences))
        {
            return packages;
        }

        foreach (var packageRef in packageReferences.EnumerateArray())
        {
            if (packageRef.TryGetProperty("Identity", out var identity) &&
                packageRef.TryGetProperty("Version", out var version))
            {
                var packageId = identity.GetString();
                var packageVersion = version.GetString();

                if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(packageVersion))
                {
                    // Filter for Aspire.Hosting.* and CommunityToolkit.Aspire.Hosting.* packages
                    if (packageId.StartsWith("Aspire.Hosting.", StringComparison.OrdinalIgnoreCase) ||
                        packageId.StartsWith("CommunityToolkit.Aspire.Hosting.", StringComparison.OrdinalIgnoreCase))
                    {
                        packages.Add(new AspirePackageInfo(packageId, packageVersion));
                    }
                }
            }
        }

        return packages;
    }

    private static async Task<List<PackageUpdateInfo>> CheckForUpdatesAsync(List<AspirePackageInfo> packages, CancellationToken cancellationToken)
    {
        // TODO: Implement sophisticated logic for checking available updates
        // For now, stub implementation that simulates finding updates for demonstration
        await Task.Delay(100, cancellationToken); // Simulate async work

        var updates = new List<PackageUpdateInfo>();

        foreach (var package in packages)
        {
            // TODO: Replace with actual package version resolution logic
            // This is a stub that assumes there's always an update available for demo purposes
            var newVersion = GetStubNewVersion(package.Version);
            if (newVersion != package.Version)
            {
                updates.Add(new PackageUpdateInfo(package.Id, package.Version, newVersion));
            }
        }

        return updates;
    }

    private static string GetStubNewVersion(string currentVersion)
    {
        // TODO: Replace with actual version resolution logic
        // This is a simple stub that increments the patch version
        if (Version.TryParse(currentVersion, out var version))
        {
            return new Version(version.Major, version.Minor, version.Build + 1).ToString();
        }

        return currentVersion;
    }

    private async Task<bool> ConfirmUpdatesAsync(List<PackageUpdateInfo> updates, CancellationToken cancellationToken)
    {
        _interactionService.DisplayEmptyLine();
        _interactionService.DisplayMessage("📦", "The following packages will be updated:");
        _interactionService.DisplayEmptyLine();

        foreach (var update in updates)
        {
            _interactionService.DisplayMessage("  ", $"{update.PackageId}: {update.CurrentVersion} → {update.NewVersion}");
        }

        _interactionService.DisplayEmptyLine();

        return await _interactionService.ConfirmAsync(
            UpdateCommandStrings.ConfirmUpdates,
            true,
            cancellationToken);
    }

    private async Task<int> UpdatePackagesAsync(FileInfo projectFile, List<PackageUpdateInfo> updates, OutputCollector outputCollector, CancellationToken cancellationToken)
    {
        return await _interactionService.ShowStatusAsync(
            UpdateCommandStrings.UpdatingPackages,
            async () =>
            {
                foreach (var update in updates)
                {
                    var updateOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = outputCollector.AppendOutput,
                        StandardErrorCallback = outputCollector.AppendError,
                    };

                    var result = await _runner.AddPackageAsync(
                        projectFile,
                        update.PackageId,
                        update.NewVersion,
                        null, // No specific source
                        updateOptions,
                        cancellationToken);

                    if (result != 0)
                    {
                        return ExitCodeConstants.FailedToAddPackage;
                    }
                }

                // TODO: Update AspireHostingSDKVersion in csproj XML if needed
                // This would require parsing and modifying the project file XML

                return ExitCodeConstants.Success;
            }
        );
    }

    private sealed record AspirePackageInfo(string Id, string Version);
    private sealed record PackageUpdateInfo(string PackageId, string CurrentVersion, string NewVersion);
}