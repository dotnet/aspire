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
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands;

internal sealed class InitCommand : BaseCommand, IPackageMetaPrefetchingCommand
{
    private readonly IDotNetCliRunner _runner;
    private readonly ICertificateService _certificateService;
    private readonly INewCommandPrompter _prompter;
    private readonly ITemplateFactory _templateFactory;
    private readonly IPackagingService _packagingService;
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
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(sdkInstaller);

        _runner = runner;
        _certificateService = certificateService;
        _prompter = prompter;
        _templateFactory = templateFactory;
        _packagingService = packagingService;
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

        // Check for solution files (.sln or .slnx) in the current directory
        var solutionFile = FindSolutionFile(_executionContext.WorkingDirectory);

        if (solutionFile is not null)
        {
            InteractionService.DisplayMessage("information", string.Format(CultureInfo.CurrentCulture, InitCommandStrings.SolutionDetected, solutionFile.Name));
            return await InitializeExistingSolutionAsync(solutionFile, parseResult, cancellationToken);
        }
        else
        {
            InteractionService.DisplayMessage("warning", InitCommandStrings.NoSolutionFoundCreatingSingleFileAppHost);
            return await CreateSingleFileAppHostAsync(parseResult, cancellationToken);
        }
    }

    private static FileInfo? FindSolutionFile(DirectoryInfo directory)
    {
        // Look for .sln files first, then .slnx files
        var solutionFiles = directory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
        if (solutionFiles.Length > 0)
        {
            return solutionFiles[0]; // Use the first .sln file found
        }

        var slnxFiles = directory.GetFiles("*.slnx", SearchOption.TopDirectoryOnly);
        if (slnxFiles.Length > 0)
        {
            return slnxFiles[0]; // Use the first .slnx file found
        }

        return null;
    }

    private async Task<int> InitializeExistingSolutionAsync(FileInfo solutionFile, ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Create AppHost project
        InteractionService.DisplayMessage("construction", InitCommandStrings.CreatingAppHostProject);
        var appHostTemplate = _templateFactory.GetAllTemplates().First(t => t.Name == "aspire-apphost");
        var appHostResult = await appHostTemplate.ApplyTemplateAsync(parseResult, cancellationToken);
        
        if (appHostResult.ExitCode != 0)
        {
            return appHostResult.ExitCode;
        }

        // Create ServiceDefaults project
        InteractionService.DisplayMessage("construction", InitCommandStrings.CreatingServiceDefaultsProject);
        var serviceDefaultsTemplate = _templateFactory.GetAllTemplates().First(t => t.Name == "aspire-servicedefaults");
        var serviceDefaultsResult = await serviceDefaultsTemplate.ApplyTemplateAsync(parseResult, cancellationToken);
        
        if (serviceDefaultsResult.ExitCode != 0)
        {
            return serviceDefaultsResult.ExitCode;
        }

        // Add projects to solution
        InteractionService.DisplayMessage("plus", InitCommandStrings.AddingProjectsToSolution);
        
        if (appHostResult.OutputPath is not null)
        {
            var appHostProjectFile = FindProjectFile(appHostResult.OutputPath);
            if (appHostProjectFile is not null)
            {
                var addAppHostResult = await _runner.AddProjectToSolutionAsync(
                    solutionFile, 
                    appHostProjectFile, 
                    new DotNetCliRunnerInvocationOptions(), 
                    cancellationToken);
                
                if (addAppHostResult != 0)
                {
                    return addAppHostResult;
                }
            }
        }

        if (serviceDefaultsResult.OutputPath is not null)
        {
            var serviceDefaultsProjectFile = FindProjectFile(serviceDefaultsResult.OutputPath);
            if (serviceDefaultsProjectFile is not null)
            {
                var addServiceDefaultsResult = await _runner.AddProjectToSolutionAsync(
                    solutionFile, 
                    serviceDefaultsProjectFile, 
                    new DotNetCliRunnerInvocationOptions(), 
                    cancellationToken);
                
                if (addServiceDefaultsResult != 0)
                {
                    return addServiceDefaultsResult;
                }
            }
        }

        await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
        
        InteractionService.DisplaySuccess(InitCommandStrings.AspireInitializationComplete);
        return ExitCodeConstants.Success;
    }

    private async Task<int> CreateSingleFileAppHostAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Use single-file AppHost template if feature is enabled
        if (!_features.IsFeatureEnabled(KnownFeatures.SingleFileAppHostEnabled, false))
        {
            InteractionService.DisplayError("Single-file AppHost feature is not enabled.");
            return ExitCodeConstants.FailedToCreateNewProject;
        }

        var singleFileTemplate = _templateFactory.GetAllTemplates().FirstOrDefault(t => t.Name == "aspire-apphost-singlefile");
        if (singleFileTemplate is null)
        {
            InteractionService.DisplayError("Single-file AppHost template not found.");
            return ExitCodeConstants.FailedToCreateNewProject;
        }

        var result = await singleFileTemplate.ApplyTemplateAsync(parseResult, cancellationToken);
        
        if (result.ExitCode == 0)
        {
            await _certificateService.EnsureCertificatesTrustedAsync(_runner, cancellationToken);
            InteractionService.DisplaySuccess(InitCommandStrings.AspireInitializationComplete);
        }

        return result.ExitCode;
    }

    private static FileInfo? FindProjectFile(string directoryPath)
    {
        var directory = new DirectoryInfo(directoryPath);
        var projectFiles = directory.GetFiles("*.csproj", SearchOption.TopDirectoryOnly);
        return projectFiles.Length > 0 ? projectFiles[0] : null;
    }
}