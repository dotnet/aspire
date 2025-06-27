// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Semver;

namespace Aspire.Cli.Commands;

internal sealed class AddCommand : BaseCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;
    private readonly IInteractionService _interactionService;
    private readonly IProjectLocator _projectLocator;
    private readonly IAddCommandPrompter _prompter;
    private readonly AspireCliTelemetry _telemetry;

    public AddCommand(IDotNetCliRunner runner, INuGetPackageCache nuGetPackageCache, IInteractionService interactionService, IProjectLocator projectLocator, IAddCommandPrompter prompter, AspireCliTelemetry telemetry, IFeatures features, ICliUpdateNotififier updateNotifier)
        : base("add", AddCommandStrings.Description, features, updateNotifier)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(nuGetPackageCache);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(projectLocator);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(telemetry);

        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;
        _interactionService = interactionService;
        _projectLocator = projectLocator;
        _prompter = prompter;
        _telemetry = telemetry;

        var integrationArgument = new Argument<string>("integration");
        integrationArgument.Description = AddCommandStrings.IntegrationArgumentDescription;
        integrationArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(integrationArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = AddCommandStrings.ProjectArgumentDescription;
        Options.Add(projectOption);

        var versionOption = new Option<string>("--version", "-v");
        versionOption.Description = AddCommandStrings.VersionArgumentDescription;
        Options.Add(versionOption);

        var sourceOption = new Option<string?>("--source", "-s");
        sourceOption.Description = AddCommandStrings.SourceArgumentDescription;
        Options.Add(sourceOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        var outputCollector = new OutputCollector();

        try
        {
            var integrationName = parseResult.GetValue<string>("integration");

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = await _projectLocator.UseOrFindAppHostProjectFileAsync(passedAppHostProjectFile, cancellationToken);

            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var source = parseResult.GetValue<string?>("--source");

            var packages = await _interactionService.ShowStatusAsync(
                AddCommandStrings.SearchingForAspirePackages,
                () => _nuGetPackageCache.GetIntegrationPackagesAsync(
                    workingDirectory: effectiveAppHostProjectFile.Directory!,
                    prerelease: true,
                    source: source,
                    cancellationToken: cancellationToken)
                );

            if (!packages.Any())
            {
                throw new EmptyChoicesException(AddCommandStrings.NoIntegrationPackagesFound);
            }

            var version = parseResult.GetValue<string?>("--version");

            var packagesWithShortName = packages.Select(GenerateFriendlyName);

            if (!packagesWithShortName.Any())
            {
                _interactionService.DisplayError(AddCommandStrings.NoPackagesFound);
                return ExitCodeConstants.FailedToAddPackage;
            }

            var filteredPackagesWithShortName = packagesWithShortName.Where(p => p.FriendlyName == integrationName || p.Package.Id == integrationName);

            if (!filteredPackagesWithShortName.Any() && integrationName is not null)
            {
                // If we didn't get an exact match on the friendly name or the package ID
                // then try a contains search to created a broader filtered list.
                filteredPackagesWithShortName = packagesWithShortName.Where(
                    p => p.FriendlyName.Contains(integrationName, StringComparison.OrdinalIgnoreCase)
                    || p.Package.Id.Contains(integrationName, StringComparison.OrdinalIgnoreCase)
                    );
            }

            // If we didn't match any, show a complete list. If we matched one, and its
            // an exact match, then we still prompt, but it will only prompt for
            // the version. If there is more than one match then we prompt.
            var selectedNuGetPackage = filteredPackagesWithShortName.Count() switch {
                0 => await GetPackageByInteractiveFlow(packagesWithShortName, null, cancellationToken),
                1 => filteredPackagesWithShortName.First().Package.Version == version
                    ? filteredPackagesWithShortName.First()
                    : await GetPackageByInteractiveFlow(filteredPackagesWithShortName, null, cancellationToken),
                > 1 => await GetPackageByInteractiveFlow(filteredPackagesWithShortName, version, cancellationToken),
                _ => throw new InvalidOperationException(AddCommandStrings.UnexpectedNumberOfPackagesFound)
            };

            var addPackageResult = await _interactionService.ShowStatusAsync(
                AddCommandStrings.AddingAspireIntegration,
                async () => {

                    var addPackageOptions = new DotNetCliRunnerInvocationOptions
                    {
                        StandardOutputCallback = outputCollector.AppendOutput,
                        StandardErrorCallback = outputCollector.AppendError,
                    };
                    var addPackageResult = await _runner.AddPackageAsync(
                        effectiveAppHostProjectFile,
                        selectedNuGetPackage.Package.Id,
                        selectedNuGetPackage.Package.Version,
                        source,
                        addPackageOptions,
                        cancellationToken);

                    return addPackageResult == 0 ? ExitCodeConstants.Success : ExitCodeConstants.FailedToAddPackage;
                }
            );

            if (addPackageResult != 0)
            {
                _interactionService.DisplayLines(outputCollector.GetLines());
                _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, AddCommandStrings.PackageInstallationFailed, addPackageResult));
                return ExitCodeConstants.FailedToAddPackage;
            }
            else
            {
                _interactionService.DisplaySuccess(string.Format(CultureInfo.CurrentCulture, AddCommandStrings.PackageAddedSuccessfully, selectedNuGetPackage.Package.Id, selectedNuGetPackage.Package.Version));
                return ExitCodeConstants.Success;
            }
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
        catch (EmptyChoicesException ex)
        {
            _interactionService.DisplayError(ex.Message);
            return ExitCodeConstants.FailedToAddPackage;
        }
        catch (Exception ex)
        {
            _interactionService.DisplayLines(outputCollector.GetLines());
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, AddCommandStrings.ErrorOccurredWhileAddingPackage, ex.Message));
            return ExitCodeConstants.FailedToAddPackage;
        }
    }

    private async Task<(string FriendlyName, NuGetPackage Package)> GetPackageByInteractiveFlow(IEnumerable<(string FriendlyName, NuGetPackage Package)> possiblePackages, string? preferredVersion, CancellationToken cancellationToken)
    {
        var distinctPackages = possiblePackages.DistinctBy(p => p.Package.Id);

        // If there is only one package, we can skip the prompt and just use it.
        var selectedPackage = distinctPackages.Count() switch
        {
            1 => distinctPackages.First(),
            > 1 => await _prompter.PromptForIntegrationAsync(distinctPackages, cancellationToken),
            _ => throw new InvalidOperationException(AddCommandStrings.UnexpectedNumberOfPackagesFound)
        };

        var packageVersions = possiblePackages.Where(p => p.Package.Id == selectedPackage.Package.Id);

        // If any of the package versions are an exact match for the preferred version
        // then we can skip the version prompt and just use that version.
        if (packageVersions.Any(p => p.Package.Version == preferredVersion))
        {
            var preferredVersionPackage = packageVersions.First(p => p.Package.Version == preferredVersion);
            return preferredVersionPackage;
        }

            // ... otherwise we had better prompt.
        var orderedPackageVersions = packageVersions.OrderByDescending(p => SemVersion.Parse(p.Package.Version), SemVersion.PrecedenceComparer);
        var version = await _prompter.PromptForIntegrationVersionAsync(orderedPackageVersions, cancellationToken);

        return version;
    }

    private static (string FriendlyName, NuGetPackage Package) GenerateFriendlyName(NuGetPackage package)
    {
        var shortNameBuilder = new StringBuilder();

        if (package.Id.StartsWith("Aspire.Hosting.Azure."))
        {
            shortNameBuilder.Append("az-");
        }
        else if (package.Id.StartsWith("Aspire.Hosting.AWS."))
        {
            shortNameBuilder.Append("aws-");
        }
        else if (package.Id.StartsWith("CommunityToolkit.Aspire.Hosting."))
        {
            shortNameBuilder.Append("ct-");
        }

        var lastSegment = package.Id.Split('.').Last().ToLower();
        shortNameBuilder.Append(lastSegment);
        return (shortNameBuilder.ToString(), package);
    }
}

internal interface IAddCommandPrompter
{
    Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken);
    Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationVersionAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken);
}

internal class  AddCommandPrompter(IInteractionService interactionService) : IAddCommandPrompter
{
    public virtual async Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationVersionAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken)
    {
        var selectedPackage = packages.First();
        var version = await interactionService.PromptForSelectionAsync(
            string.Format(CultureInfo.CurrentCulture, AddCommandStrings.SelectAVersionOfPackage, selectedPackage.Package.Id),
            packages.DistinctBy(p => p.Package.Version),
            p => p.Package.Version,
            cancellationToken);
        return version;
    }

    public virtual async Task<(string FriendlyName, NuGetPackage Package)> PromptForIntegrationAsync(IEnumerable<(string FriendlyName, NuGetPackage Package)> packages, CancellationToken cancellationToken)
    {
        var selectedIntegration = await interactionService.PromptForSelectionAsync(
            AddCommandStrings.SelectAnIntegrationToAdd,
            packages,
            PackageNameWithFriendlyNameIfAvailable,
            cancellationToken);
        return selectedIntegration;
    }

    private static string PackageNameWithFriendlyNameIfAvailable((string FriendlyName, NuGetPackage Package) packageWithFriendlyName)
    {
        if (packageWithFriendlyName.FriendlyName is { } friendlyName)
        {
            return $"[bold]{friendlyName}[/] ({packageWithFriendlyName.Package.Id})";
        }
        else
        {
            return packageWithFriendlyName.Package.Id;
        }
    }
}
