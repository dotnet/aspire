// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Certificates;
using Aspire.Cli.Configuration;
using Aspire.Cli.DotNet;
using Aspire.Cli.Interaction;
using Aspire.Cli.NuGet;
using Aspire.Cli.Packaging;
using Aspire.Cli.Projects;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;
using NuGetPackage = Aspire.Shared.NuGetPackageCli;
using Semver;

namespace Aspire.Cli.Commands;

internal sealed class InitCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly ITemplateFactory _templateFactory;
    private readonly IPackagingService _packagingService;
    private readonly ISolutionLocator _solutionLocator;
    private readonly AspireCliTelemetry _telemetry;
    private readonly IDotNetSdkInstaller _sdkInstaller;
    private readonly IFeatures _features;
    private readonly ICliUpdateNotifier _updateNotifier;
    private readonly CliExecutionContext _executionContext;

    /// <summary>
    /// InitCommand prefetches template package metadata.
    /// </summary>
    public bool PrefetchesTemplatePackageMetadata => true;
    
    /// <summary>
    /// InitCommand prefetches CLI package metadata for update notifications.
    /// </summary>
    public bool PrefetchesCliPackageMetadata => true;

    public InitCommand(
        IDotNetCliRunner runner,
        ICertificateService certificateService,
        INewCommandPrompter prompter,
        ITemplateFactory templateFactory,
        IPackagingService packagingService,
        ISolutionLocator solutionLocator,
        AspireCliTelemetry telemetry,
        IDotNetSdkInstaller sdkInstaller,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService)
        : base("init", InitCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(certificateService);
        ArgumentNullException.ThrowIfNull(prompter);
        ArgumentNullException.ThrowIfNull(templateFactory);
        ArgumentNullException.ThrowIfNull(packagingService);
        ArgumentNullException.ThrowIfNull(solutionLocator);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(sdkInstaller);

        _runner = runner;
        _certificateService = certificateService;
        _prompter = prompter;
        _templateFactory = templateFactory;
        _packagingService = packagingService;
        _solutionLocator = solutionLocator;
        _telemetry = telemetry;
        _sdkInstaller = sdkInstaller;
        _features = features;
        _updateNotifier = updateNotifier;
        _executionContext = executionContext;

        var sourceOption = new Option<string?>("--source", "-s");
        sourceOption.Description = NewCommandStrings.SourceArgumentDescription;
        sourceOption.Recursive = true;
        Options.Add(sourceOption);

        var templateVersionOption = new Option<string?>("--version", "-v");
        templateVersionOption.Description = NewCommandStrings.VersionArgumentDescription;
        templateVersionOption.Recursive = true;
        Options.Add(templateVersionOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Check if the .NET SDK is available
        if (!await SdkInstallHelper.EnsureSdkInstalledAsync(_sdkInstaller, InteractionService, cancellationToken))
        {
            return ExitCodeConstants.SdkNotInstalled;
        }

        using var activity = _telemetry.ActivitySource.StartActivity(this.Name);

        // Create the init context to build up a model of the operation
        var initContext = new InitContext();

        // Use SolutionLocator to find solution files, walking up the directory tree
        initContext.SelectedSolutionFile = await _solutionLocator.FindSolutionFileAsync(_executionContext.WorkingDirectory, cancellationToken);

        if (initContext.SelectedSolutionFile is not null)
        {
            InteractionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, InitCommandStrings.SolutionDetected, initContext.SelectedSolutionFile.Name));
            return await InitializeExistingSolutionAsync(initContext, parseResult, cancellationToken);
        }
        else
        {
            InteractionService.DisplayMessage("information", InitCommandStrings.NoSolutionFoundCreatingSingleFileAppHost);
            return await CreateEmptyAppHostAsync(parseResult, cancellationToken);
        }
    }

    private async Task<int> InitializeExistingSolutionAsync(InitContext initContext, ParseResult parseResult, CancellationToken cancellationToken)
    {
        var solutionFile = initContext.SelectedSolutionFile!;
        
        // Check if AppHost and ServiceDefaults projects already exist
        if (Directory.Exists(initContext.ExpectedAppHostDirectory) || Directory.Exists(initContext.ExpectedServiceDefaultsDirectory))
        {
            InteractionService.DisplayMessage("check_mark", InitCommandStrings.SolutionAlreadyInitialized);
            return ExitCodeConstants.Success;
        }
        
        // Get template version/channel selection using the same logic as NewCommand
        var selectedTemplateDetails = await GetProjectTemplatesVersionAsync(parseResult, cancellationToken);
        
        // Use the dotnet CLI runner to create the aspire template directly
        InteractionService.DisplayMessage("construction", "Creating Aspire projects...");
        
        // Create a temporary directory for the template output
        var tempProjectDir = Path.Combine(Path.GetTempPath(), $"aspire-init-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempProjectDir);
        
        try
        {
            // Create temporary NuGet config if using explicit channel
            using var temporaryConfig = selectedTemplateDetails.Channel.Type == PackageChannelType.Explicit ? await TemporaryNuGetConfig.CreateAsync(selectedTemplateDetails.Channel.Mappings!) : null;
            
            // Install templates first if needed
            var templateInstallResult = await InteractionService.ShowStatusAsync(
                "Getting templates...",
                async () =>
                {
                    var options = new DotNetCliRunnerInvocationOptions();
                    return await _runner.InstallTemplateAsync(
                        packageName: "Aspire.ProjectTemplates",
                        version: selectedTemplateDetails.Package.Version,
                        nugetConfigFile: temporaryConfig?.ConfigFile,
                        nugetSource: selectedTemplateDetails.Package.Source,
                        force: true,
                        options: options,
                        cancellationToken: cancellationToken);
                });
            
            if (templateInstallResult.ExitCode != 0)
            {
                InteractionService.DisplayError("Failed to install Aspire templates.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }
            
            // Apply the aspire template directly using the CLI runner with solution name
            var createResult = await _runner.NewProjectAsync(
                "aspire", 
                initContext.SolutionName, 
                tempProjectDir, 
                [], // No extra args needed for aspire template
                new DotNetCliRunnerInvocationOptions(), 
                cancellationToken);
            
            if (createResult != 0)
            {
                return createResult;
            }

            // Find the created projects in the temporary directory
            var tempDir = new DirectoryInfo(tempProjectDir);
            var appHostProjects = tempDir.GetDirectories("*.AppHost", SearchOption.TopDirectoryOnly);
            var serviceDefaultsProjects = tempDir.GetDirectories("*.ServiceDefaults", SearchOption.TopDirectoryOnly);

            if (appHostProjects.Length == 0 || serviceDefaultsProjects.Length == 0)
            {
                InteractionService.DisplayError("Failed to find created AppHost or ServiceDefaults projects in template output.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }

            var appHostProjectDir = appHostProjects[0];
            var serviceDefaultsProjectDir = serviceDefaultsProjects[0];

            // Move the projects to the solution directory
            var finalAppHostDir = Path.Combine(initContext.SolutionDirectory.FullName, appHostProjectDir.Name);
            var finalServiceDefaultsDir = Path.Combine(initContext.SolutionDirectory.FullName, serviceDefaultsProjectDir.Name);

            Directory.Move(appHostProjectDir.FullName, finalAppHostDir);
            Directory.Move(serviceDefaultsProjectDir.FullName, finalServiceDefaultsDir);

            // Add projects to solution
            var addResult = await InteractionService.ShowStatusAsync(
                InitCommandStrings.AddingProjectsToSolution,
                async () =>
                {
                    var appHostProjectFile = new FileInfo(Path.Combine(finalAppHostDir, $"{appHostProjectDir.Name}.csproj"));
                    var serviceDefaultsProjectFile = new FileInfo(Path.Combine(finalServiceDefaultsDir, $"{serviceDefaultsProjectDir.Name}.csproj"));

                    var addAppHostResult = await _runner.AddProjectToSolutionAsync(
                        solutionFile, 
                        appHostProjectFile, 
                        new DotNetCliRunnerInvocationOptions(), 
                        cancellationToken);
                    
                    if (addAppHostResult != 0)
                    {
                        return addAppHostResult;
                    }

                    var addServiceDefaultsResult = await _runner.AddProjectToSolutionAsync(
                        solutionFile, 
                        serviceDefaultsProjectFile, 
                        new DotNetCliRunnerInvocationOptions(), 
                        cancellationToken);
                    
                    return addServiceDefaultsResult;
                });
            
            if (addResult != 0)
            {
                return addResult;
            }

            // Get list of projects in solution
            var (getSolutionExitCode, solutionProjects) = await _runner.GetSolutionProjectsAsync(
                solutionFile,
                new DotNetCliRunnerInvocationOptions(),
                cancellationToken);

            if (getSolutionExitCode != 0)
            {
                InteractionService.DisplayError("Failed to get projects from solution.");
                return getSolutionExitCode;
            }

            // Find the appHost project we just added
            var appHostProjectFile = new FileInfo(Path.Combine(finalAppHostDir, $"{appHostProjectDir.Name}.csproj"));
            
            // Find executable projects (excluding the appHost we just added)
            var executableProjects = new List<FileInfo>();
            foreach (var project in solutionProjects)
            {
                // Skip the appHost project we just added
                if (project.FullName.Equals(appHostProjectFile.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Get OutputType property
                var (exitCode, jsonDoc) = await _runner.GetProjectItemsAndPropertiesAsync(
                    project,
                    [],
                    ["OutputType"],
                    new DotNetCliRunnerInvocationOptions(),
                    cancellationToken);

                if (exitCode == 0 && jsonDoc != null)
                {
                    var rootElement = jsonDoc.RootElement;
                    if (rootElement.TryGetProperty("Properties", out var properties))
                    {
                        if (properties.TryGetProperty("OutputType", out var outputTypeElement))
                        {
                            var outputType = outputTypeElement.GetString();
                            if (outputType == "Exe" || outputType == "WinExe")
                            {
                                executableProjects.Add(project);
                            }
                        }
                    }
                }
            }

            // If there are executable projects, prompt user to select which ones to add to appHost
            if (executableProjects.Count > 0)
            {
                InteractionService.DisplayMessage("information", "Found executable projects in solution:");
                foreach (var project in executableProjects)
                {
                    InteractionService.DisplaySubtleMessage($"  - {Path.GetFileNameWithoutExtension(project.Name)}");
                }

                var selectedProjects = await InteractionService.PromptForSelectionsAsync(
                    "Select projects to add to the AppHost:",
                    executableProjects,
                    project => Path.GetFileNameWithoutExtension(project.Name),
                    cancellationToken);

                // Add selected projects to appHost
                if (selectedProjects.Count > 0)
                {
                    InteractionService.DisplayMessage("construction", "Adding project references to AppHost...");
                    
                    foreach (var project in selectedProjects)
                    {
                        var addRefResult = await _runner.AddProjectReferenceAsync(
                            appHostProjectFile,
                            project,
                            new DotNetCliRunnerInvocationOptions(),
                            cancellationToken);

                        if (addRefResult != 0)
                        {
                            InteractionService.DisplayError($"Failed to add reference to {Path.GetFileNameWithoutExtension(project.Name)}.");
                            return addRefResult;
                        }

                        InteractionService.DisplaySubtleMessage($"  Added reference to {Path.GetFileNameWithoutExtension(project.Name)}");
                    }
                }
            }

            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
            
            InteractionService.DisplaySuccess(InitCommandStrings.AspireInitializationComplete);
            return ExitCodeConstants.Success;
        }
        finally
        {
            // Clean up temporary directory
            if (Directory.Exists(tempProjectDir))
            {
                Directory.Delete(tempProjectDir, recursive: true);
            }
        }
    }

    private async Task<int> CreateEmptyAppHostAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        ITemplate template;
        
        if (_features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
        {
            // Use single-file AppHost template if feature is enabled
            var singleFileTemplate = _templateFactory.GetTemplates().FirstOrDefault(t => t.Name == "aspire-apphost-singlefile");
            if (singleFileTemplate is null)
            {
                InteractionService.DisplayError("Single-file AppHost template not found.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }
            template = singleFileTemplate;
        }
        else
        {
            // Use regular AppHost template if single-file feature is not enabled
            var appHostTemplate = _templateFactory.GetTemplates().FirstOrDefault(t => t.Name == "aspire-apphost");
            if (appHostTemplate is null)
            {
                InteractionService.DisplayError("AppHost template not found.");
                return ExitCodeConstants.FailedToCreateNewProject;
            }
            template = appHostTemplate;
        }

        var result = await template.ApplyTemplateAsync(parseResult, cancellationToken);
        
        if (result.ExitCode == 0)
        {
            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
            InteractionService.DisplaySuccess(InitCommandStrings.AspireInitializationComplete);
        }

        return result.ExitCode;
    }

    private async Task<(NuGetPackage Package, PackageChannel Channel)> GetProjectTemplatesVersionAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var channels = await _packagingService.GetChannelsAsync(cancellationToken);

        var packagesFromChannels = await InteractionService.ShowStatusAsync("Searching for available template versions...", async () =>
        {
            var results = new List<(NuGetPackage Package, PackageChannel Channel)>();
            var packagesFromChannelsLock = new object();

            await Parallel.ForEachAsync(channels, cancellationToken, async (channel, ct) =>
            {
                var templatePackages = await channel.GetTemplatePackagesAsync(_executionContext.WorkingDirectory, ct);
                lock (packagesFromChannelsLock)
                {
                    results.AddRange(templatePackages.Select(p => (p, channel)));
                }
            });

            return results;
        });

        if (!packagesFromChannels.Any())
        {
            throw new InvalidOperationException("No template versions found");
        }

        var orderedPackagesFromChannels = packagesFromChannels.OrderByDescending(p => SemVersion.Parse(p.Package.Version), SemVersion.PrecedenceComparer);

        // Check for explicit version specified via command line
        if (parseResult.GetValue<string>("--version") is { } version)
        {
            var explicitPackageFromChannel = orderedPackagesFromChannels.FirstOrDefault(p => p.Package.Version == version);
            if (explicitPackageFromChannel.Package is not null)
            {
                return explicitPackageFromChannel;
            }
        }

        // Prompt user to select from available versions/channels
        var selectedPackageFromChannel = await _prompter.PromptForTemplatesVersionAsync(orderedPackagesFromChannels, cancellationToken);
        return selectedPackageFromChannel;
    }
}

/// <summary>
/// Context class for building up a model of the init operation before executing changes.
/// </summary>
internal sealed class InitContext
{
    /// <summary>
    /// The solution file selected for initialization, or null if no solution was found.
    /// </summary>
    public FileInfo? SelectedSolutionFile { get; set; }

    /// <summary>
    /// Gets the solution name (without extension) derived from the selected solution file.
    /// </summary>
    public string SolutionName => Path.GetFileNameWithoutExtension(SelectedSolutionFile!.Name);

    /// <summary>
    /// Gets the directory containing the solution file.
    /// </summary>
    public DirectoryInfo SolutionDirectory => SelectedSolutionFile!.Directory!;

    /// <summary>
    /// Gets the expected directory path for the AppHost project.
    /// </summary>
    public string ExpectedAppHostDirectory => Path.Combine(SolutionDirectory.FullName, $"{SolutionName}.AppHost");

    /// <summary>
    /// Gets the expected directory path for the ServiceDefaults project.
    /// </summary>
    public string ExpectedServiceDefaultsDirectory => Path.Combine(SolutionDirectory.FullName, $"{SolutionName}.ServiceDefaults");
}