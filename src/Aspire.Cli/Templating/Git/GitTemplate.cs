// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.Logging;
using Spectre.Console;

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

    public TemplateRuntime Runtime => TemplateRuntime.Cli;

    public Func<string, string> PathDeriver => name => name;

    public bool SupportsLanguage(string languageId) => true;

    public IReadOnlyList<string> SelectableAppHostLanguages => [];

    public void ApplyOptions(Command command)
    {
        // Git template variables are discovered at runtime from the manifest, not at command
        // construction time. We allow unmatched tokens so users can pass --varName value pairs
        // that will be matched against manifest variables during template application.
        command.TreatUnmatchedTokensAsErrors = false;
    }

    public async Task<TemplateResult> ApplyTemplateAsync(
        TemplateInputs inputs,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var projectName = inputs.Name ?? Path.GetFileName(Directory.GetCurrentDirectory());
        var outputDir = inputs.Output ?? Path.Combine(Directory.GetCurrentDirectory(), projectName);
        outputDir = Path.GetFullPath(outputDir);

        // Parse CLI-provided variable values from unmatched tokens (e.g., --useRedis true --port 5432)
        var cliValues = ParseUnmatchedTokens(parseResult);

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

                    // Check if the variable was provided on the CLI
                    if (TryGetCliValue(cliValues, varName, out var cliValue))
                    {
                        var validationError = ValidateCliValue(varName, cliValue, varDef);
                        if (validationError is not null)
                        {
                            _interactionService.DisplayError(validationError);
                            return new TemplateResult(1);
                        }
                        variables[varName] = cliValue;
                        continue;
                    }

                    var promptText = varDef.DisplayName?.Resolve() ?? varName;
                    var value = await PromptForVariableAsync(promptText, varDef, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Parses unmatched CLI tokens into a dictionary of key-value pairs.
    /// Supports <c>--key value</c> and bare <c>--flag</c> (treated as boolean true).
    /// </summary>
    private static Dictionary<string, string> ParseUnmatchedTokens(ParseResult parseResult)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var tokens = parseResult.UnmatchedTokens;

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (!token.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = token[2..]; // Strip leading --
            if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                result[key] = tokens[i + 1];
                i++; // Skip the value token
            }
            else
            {
                // Bare flag (e.g., --useRedis with no value) → treat as boolean true
                result[key] = "true";
            }
        }

        return result;
    }

    /// <summary>
    /// Attempts to find a CLI-provided value for a manifest variable, supporting both
    /// camelCase (e.g., <c>--useRedis</c>) and kebab-case (e.g., <c>--use-redis</c>) naming.
    /// </summary>
    private static bool TryGetCliValue(Dictionary<string, string> cliValues, string varName, out string value)
    {
        // Try exact match first (camelCase)
        if (cliValues.TryGetValue(varName, out value!))
        {
            return true;
        }

        // Try kebab-case conversion: "useRedisCache" → "use-redis-cache"
        var kebab = ToKebabCase(varName);
        if (cliValues.TryGetValue(kebab, out value!))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    /// <summary>
    /// Converts a camelCase string to kebab-case.
    /// </summary>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }

    /// <summary>
    /// Validates a CLI-provided value against the variable definition.
    /// Returns an error message if invalid, or <c>null</c> if valid.
    /// </summary>
    private static string? ValidateCliValue(string varName, string value, GitTemplateVariable varDef)
    {
        switch (varDef.Type.ToLowerInvariant())
        {
            case "boolean":
                if (!bool.TryParse(value, out _))
                {
                    return $"Invalid value '{value}' for variable '{varName}'. Expected 'true' or 'false'.";
                }
                break;

            case "choice" when varDef.Choices is { Count: > 0 }:
                var validValues = varDef.Choices.Select(c => c.Value).ToList();
                if (!validValues.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    return $"Invalid value '{value}' for variable '{varName}'. Valid choices are: {string.Join(", ", validValues)}";
                }
                break;

            case "integer":
                if (!int.TryParse(value, CultureInfo.InvariantCulture, out var parsed))
                {
                    return $"Invalid value '{value}' for variable '{varName}'. Expected an integer.";
                }
                if (varDef.Validation?.Min is { } min && parsed < min)
                {
                    return $"Value '{value}' for variable '{varName}' must be at least {min}.";
                }
                if (varDef.Validation?.Max is { } max && parsed > max)
                {
                    return $"Value '{value}' for variable '{varName}' must be at most {max}.";
                }
                break;

            default: // "string" or unknown
                if (varDef.Validation?.Pattern is { } pattern)
                {
                    var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                    if (!regex.IsMatch(value))
                    {
                        return varDef.Validation.Message ?? $"Value '{value}' for variable '{varName}' must match pattern: {pattern}";
                    }
                }
                break;
        }

        return null;
    }

    private async Task<string> PromptForVariableAsync(string promptText, GitTemplateVariable varDef, CancellationToken cancellationToken)
    {
        switch (varDef.Type.ToLowerInvariant())
        {
            case "boolean":
                var boolDefault = varDef.DefaultValue is true;
                var boolResult = await _interactionService.ConfirmAsync(promptText, boolDefault, cancellationToken).ConfigureAwait(false);
                return boolResult.ToString().ToLowerInvariant();

            case "choice" when varDef.Choices is { Count: > 0 }:
                var selected = await _interactionService.PromptForSelectionAsync(
                    promptText,
                    varDef.Choices,
                    choice => choice.DisplayName?.Resolve() ?? choice.Value,
                    cancellationToken).ConfigureAwait(false);
                return selected.Value;

            case "integer":
                var intDefault = varDef.DefaultValue?.ToString();
                var intMin = varDef.Validation?.Min;
                var intMax = varDef.Validation?.Max;
                return await _interactionService.PromptForStringAsync(
                    promptText,
                    defaultValue: intDefault,
                    validator: input =>
                    {
                        if (!int.TryParse(input, CultureInfo.InvariantCulture, out var parsed))
                        {
                            return ValidationResult.Error("Value must be an integer.");
                        }
                        if (intMin.HasValue && parsed < intMin.Value)
                        {
                            return ValidationResult.Error($"Value must be at least {intMin.Value}.");
                        }
                        if (intMax.HasValue && parsed > intMax.Value)
                        {
                            return ValidationResult.Error($"Value must be at most {intMax.Value}.");
                        }
                        return ValidationResult.Success();
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);

            default: // "string" or unknown types
                var strDefault = varDef.DefaultValue?.ToString();
                var pattern = varDef.Validation?.Pattern;
                var validationMessage = varDef.Validation?.Message;
                Func<string, ValidationResult>? validator = null;

                if (pattern is not null)
                {
                    var regex = new Regex(pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                    validator = input =>
                    {
                        if (!regex.IsMatch(input))
                        {
                            return ValidationResult.Error(validationMessage ?? $"Value must match pattern: {pattern}");
                        }
                        return ValidationResult.Success();
                    };
                }

                return await _interactionService.PromptForStringAsync(
                    promptText,
                    defaultValue: strDefault,
                    validator: validator,
                    required: varDef.Required == true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> FetchTemplateAsync(string targetDir, CancellationToken cancellationToken)
    {
        var repo = _resolved.EffectiveRepo;
        var templatePath = _resolved.Entry.Path;

        // For local sources, copy files directly instead of git clone.
        if (IsLocalPath(repo))
        {
            return CopyLocalTemplate(repo, templatePath, targetDir);
        }

        var gitRef = _resolved.Source.Ref ?? "HEAD";

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

    private bool CopyLocalTemplate(string repoPath, string templatePath, string targetDir)
    {
        try
        {
            // Resolve the template path relative to the repo (index) directory
            var sourceDir = Path.GetFullPath(Path.Combine(repoPath, templatePath));

            if (!Directory.Exists(sourceDir))
            {
                _logger.LogError("Local template directory not found: {Path}.", sourceDir);
                return false;
            }

            CopyDirectoryRecursive(sourceDir, targetDir);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy local template from {Repo}/{Path}.", repoPath, templatePath);
            return false;
        }
    }

    private static void CopyDirectoryRecursive(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            if (string.Equals(dirName, ".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            CopyDirectoryRecursive(dir, Path.Combine(destDir, dirName));
        }
    }

    private static bool IsLocalPath(string path)
    {
        return path.StartsWith('/') ||
               path.StartsWith('.') ||
               (path.Length >= 3 && path[1] == ':' && (path[2] == '/' || path[2] == '\\'));
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
