// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Text;
using Aspire.Cli;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
public static class AddCommand
{
    public static void ConfigureAddCommand(Command parentCommand)
    {

        var command = new Command("add", "Add an integration or other resource to the Aspire project.");

        var resourceArgument = new Argument<string>("resource");
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        command.Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Validators.Add(BaseCommand.ValidateProjectOption);
        command.Options.Add(projectOption);

        var versionOption = new Option<string>("--version", "-v");
        command.Options.Add(versionOption);

        var prereleaseOption = new Option<bool>("--prerelease");
        command.Options.Add(prereleaseOption);

        var nugetConfigOption = new Option<bool>("--use-nuget-config");
        command.Options.Add(nugetConfigOption);

        command.SetAction(async (parseResult, ct) => {

            try
            {
                var app = Program.BuildApplication(parseResult);
                
                var integrationLookup = app.Services.GetRequiredService<INuGetPackageCache>();

                var integrationName = parseResult.GetValue<string>("resource");

                var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
                var effectiveAppHostProjectFile = BaseCommand.UseOrFindAppHostProjectFile(passedAppHostProjectFile);

                var prerelease = parseResult.GetValue<bool>("--prerelease");

                var nugetConfigRequested = parseResult.GetValue<bool>("--use-nuget-config");
                var nugetSource = nugetConfigRequested ? null : "https://api.nuget.org/v3/index.json";
                if (nugetConfigRequested)
                {
                    
                    AnsiConsole.MarkupLine("[yellow]Using nearest nuget.config[/]");
                }

                var packages = await AnsiConsole.Status().StartAsync(
                    "Searching for Aspire packages...",
                    context => integrationLookup.GetPackagesAsync(effectiveAppHostProjectFile, prerelease, nugetSource, ct)
                    ).ConfigureAwait(false);

                var packagesWithShortName = packages.Select(p => GenerateFriendlyName(p));

                var selectedNuGetPackage = packagesWithShortName.FirstOrDefault(p => p.FriendlyName == integrationName || p.Package.Id == integrationName);

                if (selectedNuGetPackage == default)
                {
                    selectedNuGetPackage = GetPackageByInteractiveFlow(packagesWithShortName);
                }
                else
                {
                    // If we find an exact match we will use it, but override the version
                    // if the version option is specified.
                    var version = parseResult.GetValue<string?>("--version");
                    if (version is not null)
                    {
                        selectedNuGetPackage.Package.Version = version;
                    }
                }

                var addPackageResult = await AnsiConsole.Status().StartAsync(
                    "Adding Aspire integration...",
                    async context => {
                        var runner = app.Services.GetRequiredService<DotNetCliRunner>();
                        var addPackageResult = await runner.AddPackageAsync(
                            effectiveAppHostProjectFile,
                            selectedNuGetPackage.Package.Id,
                            selectedNuGetPackage.Package.Version,
                            ct
                            ).ConfigureAwait(false);

                        return addPackageResult == 0 ? ExitCodeConstants.Success : ExitCodeConstants.FailedToAddPackage;
                    }                
                ).ConfigureAwait(false);

                return addPackageResult;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red bold]:thumbs_down: An error occurred while adding the package: {ex.Message}[/]");
                return ExitCodeConstants.FailedToAddPackage;
            }
        });

        parentCommand.Subcommands.Add(command);
    }

    private static (string FriendlyName, NuGetPackage Package) GetPackageByInteractiveFlow(IEnumerable<(string FriendlyName, NuGetPackage Package)> knownPackages)
    {
        var packagePrompt = new SelectionPrompt<(string FriendlyName, NuGetPackage Package)>()
            .Title("Select an integration to add:")
            .UseConverter(PackageNameWithFriendlyNameIfAvailable)
            .PageSize(10)
            .EnableSearch()
            .HighlightStyle(Style.Parse("darkmagenta"))
            .AddChoices(knownPackages);

        var selectedIntegration = AnsiConsole.Prompt(packagePrompt);

        var versionPrompt = new TextPrompt<string>("Specify a version of the integration:")
            .DefaultValue(selectedIntegration.Package.Version)
            .Validate(value => string.IsNullOrEmpty(value) ? ValidationResult.Error("Version cannot be empty.") : ValidationResult.Success())
            .ShowDefaultValue(true)
            .DefaultValueStyle(Style.Parse("darkmagenta"));

        var version = AnsiConsole.Prompt(versionPrompt);

        selectedIntegration.Package.Version = version;

        return selectedIntegration;

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