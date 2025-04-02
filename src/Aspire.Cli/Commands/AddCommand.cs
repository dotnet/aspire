// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class AddCommand : BaseCommand
{
    private readonly ActivitySource _activitySource = new ActivitySource("Aspire.Cli");
    private readonly DotNetCliRunner _runner;
    private readonly INuGetPackageCache _nuGetPackageCache;

    public AddCommand(DotNetCliRunner runner, INuGetPackageCache nuGetPackageCache) : base("add", "Add an integration or other resource to the Aspire project.")
    {
        ArgumentNullException.ThrowIfNull(runner, nameof(runner));
        ArgumentNullException.ThrowIfNull(nuGetPackageCache, nameof(nuGetPackageCache));
        _runner = runner;
        _nuGetPackageCache = nuGetPackageCache;

        var resourceArgument = new Argument<string>("resource");
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(ProjectFileHelper.ValidateProjectOption);
        Options.Add(projectOption);

        var versionOption = new Option<string>("--version", "-v");
        Options.Add(versionOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        Options.Add(prereleaseOption);

        var sourceOption = new Option<string?>("--source", "-s");
        Options.Add(sourceOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity($"{nameof(ExecuteAsync)}", ActivityKind.Internal);

        try
        {
            var integrationName = parseResult.GetValue<string>("resource");

            var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
            var effectiveAppHostProjectFile = ProjectFileHelper.UseOrFindAppHostProjectFile(passedAppHostProjectFile);
            
            if (effectiveAppHostProjectFile is null)
            {
                return ExitCodeConstants.FailedToFindProject;
            }

            var prerelease = parseResult.GetValue<bool>("--prerelease");

            var source = parseResult.GetValue<string?>("--source");

            var packages = await AnsiConsole.Status().StartAsync(
                "Searching for Aspire packages...",
                context => _nuGetPackageCache.GetPackagesAsync(effectiveAppHostProjectFile, prerelease, source, cancellationToken)
                );

            var version = parseResult.GetValue<string?>("--version");

            var packagesWithShortName = packages.Select(GenerateFriendlyName);

            if (!packagesWithShortName.Any())
            {
                AnsiConsole.MarkupLine("[red bold]:thumbs_down: No packages found.[/]");
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
                _ => throw new InvalidOperationException("Unexpected number of packages found.")
            };

            var addPackageResult = await AnsiConsole.Status().StartAsync(
                "Adding Aspire integration...",
                async context => {
                    var addPackageResult = await _runner.AddPackageAsync(
                        effectiveAppHostProjectFile,
                        selectedNuGetPackage.Package.Id,
                        selectedNuGetPackage.Package.Version,
                        cancellationToken
                        );

                    return addPackageResult == 0 ? ExitCodeConstants.Success : ExitCodeConstants.FailedToAddPackage;
                }
            );

            if (addPackageResult != 0)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down: The package installation failed with exit code {addPackageResult}. For more information run with --debug switch.[/]");
                return ExitCodeConstants.FailedToAddPackage;
            }
            else
            {
                AnsiConsole.MarkupLine($":thumbs_up: The package {selectedNuGetPackage.Package.Id}::{selectedNuGetPackage.Package.Version} was added successfully.");
                return ExitCodeConstants.Success;
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow bold]:stop_sign: Operation cancelled by user action.[/]");
            return ExitCodeConstants.FailedToAddPackage;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red bold]:thumbs_down: An error occurred while adding the package: {ex.Message}[/]");
            return ExitCodeConstants.FailedToAddPackage;
        }
    }

    private static async Task<(string FriendlyName, NuGetPackage Package)> GetPackageByInteractiveFlow(IEnumerable<(string FriendlyName, NuGetPackage Package)> possiblePackages, string? preferredVersion, CancellationToken cancellationToken)
    {
        var distinctPackages = possiblePackages.DistinctBy(p => p.Package.Id);

        var packagePrompt = new SelectionPrompt<(string FriendlyName, NuGetPackage Package)>()
            .Title("Select an integration to add:")
            .UseConverter(PackageNameWithFriendlyNameIfAvailable)
            .PageSize(10)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"))
            .AddChoices(distinctPackages);

        // If there is only one package, we can skip the prompt and just use it.
        var selectedPackage = distinctPackages.Count() switch
        {
            1 => distinctPackages.First(),
            > 1 => await AnsiConsole.PromptAsync(packagePrompt, cancellationToken),
            _ => throw new InvalidOperationException("Unexpected number of packages found.")
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
        var versionPrompt = new SelectionPrompt<(string FriendlyName, NuGetPackage Package)>()
            .Title($"Select a version of {selectedPackage.Package.Id}:")
            .UseConverter(p => p.Package.Version)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"))
            .AddChoices(packageVersions);

        var version = await AnsiConsole.PromptAsync(versionPrompt, cancellationToken);

        return version;

        static string PackageNameWithFriendlyNameIfAvailable((string FriendlyName, NuGetPackage Package) packageWithFriendlyName)
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