// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Templating;

internal sealed partial class CliTemplateFactory
{
    private async Task<TemplateResult> ApplyTypeScriptStarterTemplateAsync(CallbackTemplate _, TemplateInputs inputs, System.CommandLine.ParseResult parseResult, CancellationToken cancellationToken)
    {
        var projectName = inputs.Name;
        if (string.IsNullOrWhiteSpace(projectName))
        {
            var defaultName = _executionContext.WorkingDirectory.Name;
            projectName = await _prompter.PromptForProjectNameAsync(defaultName, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(inputs.Version))
        {
            _interactionService.DisplayError("Unable to determine Aspire version for the TypeScript starter template.");
            return new TemplateResult(ExitCodeConstants.InvalidCommand);
        }

        var aspireVersion = inputs.Version;
        var outputPath = inputs.Output;
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var defaultOutputPath = $"./{projectName}";
            outputPath = await _prompter.PromptForOutputPath(defaultOutputPath, cancellationToken);
        }
        outputPath = Path.GetFullPath(outputPath, _executionContext.WorkingDirectory.FullName);

        _logger.LogDebug("Applying TypeScript starter template. ProjectName: {ProjectName}, OutputPath: {OutputPath}, AspireVersion: {AspireVersion}.", projectName, outputPath, aspireVersion);

        var useLocalhostTld = await ResolveUseLocalhostTldAsync(parseResult, cancellationToken);

        TemplateResult templateResult;
        try
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            templateResult = await _interactionService.ShowStatusAsync(
                TemplatingStrings.CreatingNewProject,
                async () =>
                {
                    var projectNameLower = projectName.ToLowerInvariant();

                    // Generate random ports (matching .NET template port ranges)
                    var ports = GenerateRandomPorts();
                    var hostName = useLocalhostTld ? $"{projectNameLower}.dev.localhost" : "localhost";
                    string ApplyAllTokens(string content) => ApplyTokens(content, projectName, projectNameLower, aspireVersion, ports, hostName);
                    _logger.LogDebug("Copying embedded TypeScript starter template files to '{OutputPath}'.", outputPath);
                    await CopyTemplateTreeToDiskAsync("ts-starter", outputPath, ApplyAllTokens, cancellationToken);

                    var npmPath = PathLookupHelper.FindFullPathFromPath("npm") ?? PathLookupHelper.FindFullPathFromPath("npm.cmd");
                    if (npmPath is null)
                    {
                        _interactionService.DisplayError("npm is not installed or not found in PATH. Please install Node.js and try again.");
                        return new TemplateResult(ExitCodeConstants.InvalidCommand);
                    }

                    // Run npm install in the output directory (non-fatal — package may not be published yet)
                    _logger.LogDebug("Running npm install for TypeScript starter in '{OutputPath}'.", outputPath);
                    var npmInstallResult = await RunProcessAsync(npmPath, "install", outputPath, cancellationToken);
                    if (npmInstallResult.ExitCode != 0)
                    {
                        _interactionService.DisplaySubtleMessage("npm install had warnings or errors. You may need to run 'npm install' manually after dependencies are available.");
                        DisplayProcessOutput(npmInstallResult, treatStandardErrorAsError: false);
                    }

                    // Write channel to settings.json if available so that aspire add
                    // knows which channel to use for package resolution
                    if (!string.IsNullOrEmpty(inputs.Channel))
                    {
                        var config = AspireJsonConfiguration.Load(outputPath);
                        if (config is not null)
                        {
                            config.Channel = inputs.Channel;
                            config.Save(outputPath);
                        }
                    }

                    return new TemplateResult(ExitCodeConstants.Success, outputPath);
                }, emoji: KnownEmojis.Rocket);

            if (templateResult.ExitCode != ExitCodeConstants.Success)
            {
                return templateResult;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _interactionService.DisplayError($"Failed to create project files: {ex.Message}");
            return new TemplateResult(ExitCodeConstants.FailedToCreateNewProject);
        }

        _interactionService.DisplaySuccess($"Created TypeScript starter project at {outputPath.EscapeMarkup()}");
        _interactionService.DisplayMessage(KnownEmojis.Information, "Run 'aspire run' to start your AppHost.");

        return templateResult;
    }
}
