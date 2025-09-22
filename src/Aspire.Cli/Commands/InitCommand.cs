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

        // Use SolutionLocator to find solution files, walking up the directory tree
        var solutionFile = await _solutionLocator.FindSolutionFileAsync(_executionContext.WorkingDirectory, cancellationToken);

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

    private async Task<int> InitializeExistingSolutionAsync(FileInfo solutionFile, ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Use the "aspire" template which creates both AppHost and ServiceDefaults projects
        InteractionService.DisplayMessage("construction", "Creating Aspire projects...");
        var aspireTemplate = _templateFactory.GetAllTemplates().First(t => t.Name == "aspire");
        
        // Create a temporary directory for the template output
        var tempProjectDir = Path.Combine(Path.GetTempPath(), $"aspire-init-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempProjectDir);
        
        try
        {
            // Set up the parse result to use the temporary directory
            var tempParseResult = CreateParseResultWithOutput(parseResult, tempProjectDir);
            var aspireResult = await aspireTemplate.ApplyTemplateAsync(tempParseResult, cancellationToken);
            
            if (aspireResult.ExitCode != 0)
            {
                return aspireResult.ExitCode;
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
            var solutionDir = solutionFile.Directory!;
            var finalAppHostDir = Path.Combine(solutionDir.FullName, appHostProjectDir.Name);
            var finalServiceDefaultsDir = Path.Combine(solutionDir.FullName, serviceDefaultsProjectDir.Name);

            Directory.Move(appHostProjectDir.FullName, finalAppHostDir);
            Directory.Move(serviceDefaultsProjectDir.FullName, finalServiceDefaultsDir);

            // Add projects to solution
            InteractionService.DisplayMessage("plus", InitCommandStrings.AddingProjectsToSolution);
            
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
            
            if (addServiceDefaultsResult != 0)
            {
                return addServiceDefaultsResult;
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

    private static ParseResult CreateParseResultWithOutput(ParseResult originalResult, string outputPath)
    {
        // Create a new argument list with the output path set
        var args = new List<string> { "init" };
        
        // Copy existing arguments except output
        foreach (var token in originalResult.Tokens)
        {
            if (token.Type == System.CommandLine.Parsing.TokenType.Option && token.Value.StartsWith("--output"))
            {
                continue; // Skip existing output options
            }
            if (token.Type == System.CommandLine.Parsing.TokenType.Argument && token.Value.StartsWith("-o"))
            {
                continue; // Skip existing output options
            }
            args.Add(token.Value);
        }
        
        // Add the temporary output path
        args.Add("--output");
        args.Add(outputPath);
        
        return originalResult.CommandResult.Command.Parse(args.ToArray());
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
}