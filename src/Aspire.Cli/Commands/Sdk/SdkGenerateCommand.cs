// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands.Sdk;

/// <summary>
/// Command for generating SDKs from Aspire integration libraries.
/// Takes an integration class library (with [AspireExport] attributes) and generates
/// typed SDK code for other languages.
/// 
/// Usage: aspire sdk generate ./MyIntegration.csproj -l typescript -o ./output
/// </summary>
internal sealed class SdkGenerateCommand : BaseCommand
{
    private readonly IConfiguration _configuration;
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IAppHostServerProjectFactory _appHostServerProjectFactory;
    private readonly ILogger<SdkGenerateCommand> _logger;

    private static readonly Argument<FileInfo> s_integrationArgument = new("integration")
    {
        Description = "Path to the integration project (.csproj) to generate SDK from"
    };
    private static readonly Option<string> s_languageOption = new("--language", "-l")
    {
        Description = "Target language for SDK generation (e.g., typescript)",
        Required = true
    };
    private static readonly Option<DirectoryInfo> s_outputOption = new("--output", "-o")
    {
        Description = "Output directory for generated SDK files",
        Required = true
    };

    public SdkGenerateCommand(
        IConfiguration configuration,
        ILanguageDiscovery languageDiscovery,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        ILogger<SdkGenerateCommand> logger,
        AspireCliTelemetry telemetry)
        : base("generate", "Generate typed SDKs from an Aspire integration library for use in other languages.", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _configuration = configuration;
        _languageDiscovery = languageDiscovery;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _logger = logger;

        Arguments.Add(s_integrationArgument);
        Options.Add(s_languageOption);
        Options.Add(s_outputOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        // Check if running in extension mode
        if (_configuration[KnownConfigNames.ExtensionPromptEnabled] is "true")
        {
            return await InteractiveExecuteAsync(cancellationToken);
        }

        var integrationProject = parseResult.GetValue(s_integrationArgument)!;
        var language = parseResult.GetValue(s_languageOption)!;
        var outputDir = parseResult.GetValue(s_outputOption)!;

        return await ExecuteAsync(integrationProject, language, outputDir, cancellationToken);
    }

    private async Task<int> InteractiveExecuteAsync(CancellationToken cancellationToken)
    {
        // Step 1: Prompt for integration project
        var currentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        var csprojFiles = currentDirectory.EnumerateFiles("*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                       !f.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        FileInfo integrationProject;
        if (csprojFiles.Count == 0)
        {
            InteractionService.DisplayError("No .csproj files found in the current directory or subdirectories.");
            return ExitCodeConstants.FailedToFindProject;
        }
        else if (csprojFiles.Count == 1)
        {
            integrationProject = csprojFiles[0];
            InteractionService.DisplayMessage("information", $"Using project: {integrationProject.Name}");
        }
        else
        {
            // Prompt user to select a project
            integrationProject = await InteractionService.PromptForSelectionAsync(
                "Select the integration project to generate SDK from:",
                csprojFiles,
                f => f.Name,
                cancellationToken);
        }

        // Step 2: Prompt for target language
        var availableLanguages = (await _languageDiscovery.GetAvailableLanguagesAsync(cancellationToken))
            .Where(l => !string.IsNullOrEmpty(l.CodeGenerator)) // Filter out C# since it doesn't use code generation
            .ToList();

        if (availableLanguages.Count == 0)
        {
            InteractionService.DisplayError("No target languages available for SDK generation.");
            return ExitCodeConstants.InvalidCommand;
        }

        var selectedLanguageInfo = await InteractionService.PromptForSelectionAsync(
            "Select the target language for SDK generation:",
            availableLanguages,
            l => l.DisplayName,
            cancellationToken);

        // Step 3: Prompt for output directory
        var defaultOutputDir = Path.Combine(".", "generated-sdk", selectedLanguageInfo.CodeGenerator.ToLowerInvariant());
        var outputDirPath = await InteractionService.PromptForStringAsync(
            "Enter the output directory for generated SDK files:",
            defaultValue: defaultOutputDir,
            required: true,
            cancellationToken: cancellationToken);

        var outputDir = new DirectoryInfo(outputDirPath);

        // Execute the generation with the gathered inputs
        return await ExecuteAsync(integrationProject, selectedLanguageInfo.CodeGenerator, outputDir, cancellationToken);
    }

    private async Task<int> ExecuteAsync(FileInfo integrationProject, string language, DirectoryInfo outputDir, CancellationToken cancellationToken)
    {

        // Validate the integration project exists
        if (!integrationProject.Exists)
        {
            InteractionService.DisplayError($"Integration project not found: {integrationProject.FullName}");
            return ExitCodeConstants.FailedToFindProject;
        }

        if (!integrationProject.Extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            InteractionService.DisplayError($"Expected a .csproj file, got: {integrationProject.Extension}");
            return ExitCodeConstants.InvalidCommand;
        }

        // Resolve the language info
        var languageInfo = await GetLanguageInfoAsync(language, cancellationToken);
        if (languageInfo is null)
        {
            InteractionService.DisplayError($"Unsupported language: {language}");
            return ExitCodeConstants.InvalidCommand;
        }

        // Create output directory if it doesn't exist
        if (!outputDir.Exists)
        {
            outputDir.Create();
        }

        return await InteractionService.ShowStatusAsync(
            $":hammer: Generating {languageInfo.DisplayName} SDK from {integrationProject.Name}...",
            async () => await GenerateSdkAsync(integrationProject, languageInfo, outputDir, cancellationToken));
    }

    private async Task<LanguageInfo?> GetLanguageInfoAsync(string language, CancellationToken cancellationToken)
    {
        var languages = await _languageDiscovery.GetAvailableLanguagesAsync(cancellationToken);

        // Match by language ID or code generator name
        return languages.FirstOrDefault(l =>
            l.LanguageId.Value.StartsWith(language, StringComparison.OrdinalIgnoreCase) ||
            l.CodeGenerator.Equals(language, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<int> GenerateSdkAsync(
        FileInfo integrationProject,
        LanguageInfo languageInfo,
        DirectoryInfo outputDir,
        CancellationToken cancellationToken)
    {
        // Use a temporary directory for the AppHost server
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-sdk-gen", Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);

        try
        {
            var appHostServerProject = _appHostServerProjectFactory.Create(tempDir);
            var socketPath = appHostServerProject.GetSocketPath();

            // Get code generation package for the target language
            var codeGenPackage = await _languageDiscovery.GetPackageForLanguageAsync(languageInfo.LanguageId, cancellationToken);

            // Build packages list - include the code generator
            var packages = new List<(string Name, string Version)>();
            if (codeGenPackage is not null)
            {
                packages.Add((codeGenPackage, AppHostServerProject.DefaultSdkVersion));
            }

            _logger.LogDebug("Building AppHost server for SDK generation");

            // Create project files with the integration project reference
            await appHostServerProject.CreateProjectFilesAsync(
                AppHostServerProject.DefaultSdkVersion,
                packages,
                cancellationToken,
                additionalProjectReferences: [integrationProject.FullName]);

            var (buildSuccess, buildOutput) = await appHostServerProject.BuildAsync(cancellationToken);

            if (!buildSuccess)
            {
                InteractionService.DisplayError("Failed to build SDK generation server.");
                foreach (var (_, line) in buildOutput.GetLines())
                {
                    InteractionService.DisplayMessage("wrench", line.EscapeMarkup());
                }
                return ExitCodeConstants.FailedToBuildArtifacts;
            }

            // Start the server
            var currentPid = Environment.ProcessId;
            var (serverProcess, _) = appHostServerProject.Run(socketPath, currentPid, new Dictionary<string, string>());

            try
            {
                // Connect and generate code
                await using var rpcClient = await AppHostRpcClient.ConnectAsync(socketPath, cancellationToken);

                _logger.LogDebug("Generating {Language} SDK via RPC", languageInfo.CodeGenerator);
                var generatedFiles = await rpcClient.GenerateCodeAsync(languageInfo.CodeGenerator, cancellationToken);

                // Write generated files
                var outputDirFullPath = Path.GetFullPath(outputDir.FullName);
                foreach (var (fileName, content) in generatedFiles)
                {
                    var filePath = Path.GetFullPath(Path.Combine(outputDir.FullName, fileName));

                    // Validate path is within output directory (prevent path traversal)
                    if (!filePath.StartsWith(outputDirFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("Skipping file with invalid path: {FileName}", fileName);
                        continue;
                    }

                    var fileDirectory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(fileDirectory))
                    {
                        Directory.CreateDirectory(fileDirectory);
                    }
                    await File.WriteAllTextAsync(filePath, content, cancellationToken);
                    _logger.LogDebug("Wrote {FileName}", fileName);
                }

                InteractionService.DisplaySuccess($"Generated {generatedFiles.Count} files in {outputDir.FullName}");

                return ExitCodeConstants.Success;
            }
            finally
            {
                // Stop the server - just try to kill, catch if already exited
                try
                {
                    serverProcess.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Process already exited - this is fine
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error killing AppHost server process");
                }
            }
        }
        finally
        {
            // Clean up temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to clean up temp directory {TempDir}", tempDir);
            }
        }
    }
}
