// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Projects;
using Aspire.Cli.Utils;
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
    private readonly ILanguageDiscovery _languageDiscovery;
    private readonly IAppHostServerProjectFactory _appHostServerProjectFactory;
    private readonly ILogger<SdkGenerateCommand> _logger;

    public SdkGenerateCommand(
        ILanguageDiscovery languageDiscovery,
        IAppHostServerProjectFactory appHostServerProjectFactory,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        ILogger<SdkGenerateCommand> logger)
        : base("generate", "Generate typed SDKs from an Aspire integration library for use in other languages.", features, updateNotifier, executionContext, interactionService)
    {
        _languageDiscovery = languageDiscovery;
        _appHostServerProjectFactory = appHostServerProjectFactory;
        _logger = logger;

        // The integration project is the main input
        var integrationArgument = new Argument<FileInfo>("integration")
        {
            Description = "Path to the integration project (.csproj) to generate SDK from"
        };
        Arguments.Add(integrationArgument);

        var languageOption = new Option<string>("--language", "-l")
        {
            Description = "Target language for SDK generation (e.g., typescript)",
            Required = true
        };
        Options.Add(languageOption);

        var outputOption = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output directory for generated SDK files",
            Required = true
        };
        Options.Add(outputOption);

        var verifyOption = new Option<bool>("--verify")
        {
            Description = "Verify generated code compiles (runs tsc for TypeScript)"
        };
        Options.Add(verifyOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var integrationProject = parseResult.GetValue<FileInfo>("integration")!;
        var language = parseResult.GetValue<string>("--language")!;
        var outputDir = parseResult.GetValue<DirectoryInfo>("--output")!;
        var verify = parseResult.GetValue<bool>("--verify");

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
            async () => await GenerateSdkAsync(integrationProject, languageInfo, outputDir, verify, cancellationToken));
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
        bool verify,
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

                // Verify if requested
                if (verify)
                {
                    var verifyResult = await VerifyGeneratedCodeAsync(languageInfo, outputDir, cancellationToken);
                    if (verifyResult != 0)
                    {
                        return verifyResult;
                    }
                }

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

    private async Task<int> VerifyGeneratedCodeAsync(
        LanguageInfo languageInfo,
        DirectoryInfo outputDir,
        CancellationToken cancellationToken)
    {
        if (!languageInfo.CodeGenerator.Equals("TypeScript", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Verification not supported for {Language}", languageInfo.CodeGenerator);
            return ExitCodeConstants.Success;
        }

        return await InteractionService.ShowStatusAsync(
            ":magnifying_glass_tilted_right: Verifying TypeScript compilation...",
            async () =>
            {
                // Check if tsc is available
                var tscPath = FindExecutable("tsc");
                if (tscPath is null)
                {
                    InteractionService.DisplayMessage("warning", "[yellow]TypeScript compiler (tsc) not found. Skipping verification.[/]");
                    return ExitCodeConstants.Success;
                }

                // Create a minimal tsconfig.json for verification
                var tsconfigPath = Path.Combine(outputDir.FullName, "tsconfig.json");
                var tsconfigContent = """
                    {
                      "compilerOptions": {
                        "target": "ES2020",
                        "module": "NodeNext",
                        "moduleResolution": "NodeNext",
                        "strict": true,
                        "noEmit": true,
                        "skipLibCheck": true
                      },
                      "include": ["*.ts"]
                    }
                    """;

                var createdTsconfig = false;
                if (!File.Exists(tsconfigPath))
                {
                    await File.WriteAllTextAsync(tsconfigPath, tsconfigContent, cancellationToken);
                    createdTsconfig = true;
                }

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = tscPath,
                        Arguments = "--noEmit",
                        WorkingDirectory = outputDir.FullName,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };

                    using var process = Process.Start(psi);
                    if (process is null)
                    {
                        InteractionService.DisplayMessage("warning", "[yellow]Failed to start TypeScript compiler.[/]");
                        return ExitCodeConstants.Success;
                    }

                    var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                    var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                    await process.WaitForExitAsync(cancellationToken);

                    if (process.ExitCode != 0)
                    {
                        InteractionService.DisplayError("TypeScript compilation failed:");
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            InteractionService.DisplayMessage("information_source", output);
                        }
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            InteractionService.DisplayMessage("information_source", error);
                        }
                        return ExitCodeConstants.FailedToBuildArtifacts;
                    }

                    InteractionService.DisplaySuccess("TypeScript compilation verified successfully.");
                    return ExitCodeConstants.Success;
                }
                finally
                {
                    // Clean up tsconfig if we created it
                    if (createdTsconfig && File.Exists(tsconfigPath))
                    {
                        try
                        {
                            File.Delete(tsconfigPath);
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }
                }
            });
    }

    private static string? FindExecutable(string name)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
        {
            return null;
        }

        var extensions = OperatingSystem.IsWindows()
            ? new[] { ".cmd", ".exe", ".bat" }
            : new[] { "" };

        foreach (var path in pathVar.Split(Path.PathSeparator))
        {
            foreach (var ext in extensions)
            {
                var fullPath = Path.Combine(path, name + ext);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}
