// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// An <see cref="ITemplate"/> backed by a git-hosted template.
/// </summary>
internal sealed class GitTemplate : ITemplate
{
    private readonly ResolvedTemplate _resolved;
    private readonly IGitTemplateEngine _engine;
    private readonly IInteractionService _interactionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    public GitTemplate(
        ResolvedTemplate resolved,
        IGitTemplateEngine engine,
        IInteractionService interactionService,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        _resolved = resolved;
        _engine = engine;
        _interactionService = interactionService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string Name => _resolved.Entry.Name;

    public string Description => _resolved.Entry.Description ?? $"Git template from {_resolved.Source.Name}";

    public Func<string, string> PathDeriver => name => name;

    public void ApplyOptions(Commands.TemplateCommand command)
    {
        // Git templates don't add CLI options — variables are prompted interactively.
    }

    public async Task<TemplateResult> ApplyTemplateAsync(
        TemplateInputs inputs,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var projectName = inputs.Name ?? Path.GetFileName(Directory.GetCurrentDirectory());
        var outputDir = inputs.Output ?? Path.Combine(Directory.GetCurrentDirectory(), projectName);
        outputDir = Path.GetFullPath(outputDir);

        // Fetch the template content to a temp directory
        var tempDir = Path.Combine(Path.GetTempPath(), "aspire-git-templates", Guid.NewGuid().ToString("N"));

        try
        {
            var fetched = await _interactionService.ShowStatusAsync(
                $":package: Fetching template '{Name}'...",
                () => FetchTemplateAsync(tempDir, cancellationToken));

            if (!fetched)
            {
                return new TemplateResult(1);
            }

            // Read manifest to discover variables
            var manifestPath = Path.Combine(tempDir, "aspire-template.json");
            GitTemplateManifest? manifest = null;

            if (File.Exists(manifestPath))
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
                manifest = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
            }

            // Collect variable values
            var variables = new Dictionary<string, string>
            {
                ["projectName"] = projectName
            };

            if (manifest?.Variables is not null)
            {
                foreach (var (varName, varDef) in manifest.Variables)
                {
                    if (variables.ContainsKey(varName))
                    {
                        continue;
                    }

                    var defaultValue = varDef.DefaultValue?.ToString();
                    var value = await _interactionService.PromptForStringAsync(
                        varName,
                        defaultValue: defaultValue,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    variables[varName] = value;
                }
            }

            // Apply the template
            await _engine.ApplyAsync(tempDir, outputDir, variables, cancellationToken).ConfigureAwait(false);

            _interactionService.DisplaySuccess($"Created project at {outputDir}");
            return new TemplateResult(0, outputDir);
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
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private async Task<bool> FetchTemplateAsync(string targetDir, CancellationToken cancellationToken)
    {
        var repo = _resolved.EffectiveRepo;
        var gitRef = _resolved.Source.Ref ?? "HEAD";
        var templatePath = _resolved.Entry.Path;

        // Use git clone with sparse checkout for the template path
        Directory.CreateDirectory(targetDir);

        try
        {
            var cloneResult = await RunGitAsync(
                targetDir,
                ["clone", "--depth", "1", "--branch", gitRef, "--sparse", "--filter=blob:none", repo, "."],
                cancellationToken).ConfigureAwait(false);

            if (cloneResult != 0)
            {
                // Fall back to cloning without --branch (for refs like refs/pull/123/head)
                cloneResult = await RunGitAsync(
                    targetDir,
                    ["clone", "--depth", "1", "--sparse", "--filter=blob:none", repo, "."],
                    cancellationToken).ConfigureAwait(false);

                if (cloneResult != 0)
                {
                    _logger.LogError("Failed to clone repository {Repo}.", repo);
                    return false;
                }

                // Fetch the specific ref
                await RunGitAsync(targetDir, ["fetch", "origin", gitRef], cancellationToken).ConfigureAwait(false);
                await RunGitAsync(targetDir, ["checkout", "FETCH_HEAD"], cancellationToken).ConfigureAwait(false);
            }

            if (templatePath is not "." and not "")
            {
                await RunGitAsync(
                    targetDir,
                    ["sparse-checkout", "set", templatePath],
                    cancellationToken).ConfigureAwait(false);
            }

            // Move template files from subdirectory to target root if needed
            var templateSubDir = Path.Combine(targetDir, templatePath);
            if (templatePath is not "." and not "" && Directory.Exists(templateSubDir))
            {
                var tempMove = targetDir + "_move";
                Directory.Move(templateSubDir, tempMove);

                // Clear the original dir (except .git)
                foreach (var entry in Directory.GetFileSystemEntries(targetDir))
                {
                    var name = Path.GetFileName(entry);
                    if (name == ".git" || entry == tempMove)
                    {
                        continue;
                    }

                    if (Directory.Exists(entry))
                    {
                        Directory.Delete(entry, recursive: true);
                    }
                    else
                    {
                        File.Delete(entry);
                    }
                }

                // Move template files back to root
                foreach (var entry in Directory.GetFileSystemEntries(tempMove))
                {
                    var dest = Path.Combine(targetDir, Path.GetFileName(entry));
                    if (Directory.Exists(entry))
                    {
                        Directory.Move(entry, dest);
                    }
                    else
                    {
                        File.Move(entry, dest);
                    }
                }

                Directory.Delete(tempMove, recursive: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch template from {Repo}.", repo);
            return false;
        }
    }

    private static async Task<int> RunGitAsync(string workingDir, string[] args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            return -1;
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
