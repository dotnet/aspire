// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Templating.Git;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Commands.Template;

internal sealed class TemplateTestCommand : BaseTemplateSubCommand
{
    private static readonly Argument<string?> s_pathArgument = new("path")
    {
        Description = "Path to a template directory containing aspire-template.json (defaults to current directory)",
        Arity = ArgumentArity.ZeroOrOne
    };

    private static readonly Option<string> s_outputOption = new("--output", "-o")
    {
        Description = "Base directory for generated variant projects (required)",
        Required = true
    };

    private static readonly Option<string?> s_nameOption = new("--name")
    {
        Description = "Template name when path contains an aspire-template-index.json with multiple templates"
    };

    private static readonly Option<bool> s_dryRunOption = new("--dry-run")
    {
        Description = "List all combinations without applying the template"
    };

    private static readonly Option<bool> s_jsonOption = new("--json")
    {
        Description = "Output results as JSON"
    };

    private readonly IGitTemplateEngine _engine;

    public TemplateTestCommand(
        IGitTemplateEngine engine,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        AspireCliTelemetry telemetry)
        : base("test", "Test a template by generating all variable combinations", features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _engine = engine;
        Arguments.Add(s_pathArgument);
        Options.Add(s_outputOption);
        Options.Add(s_nameOption);
        Options.Add(s_dryRunOption);
        Options.Add(s_jsonOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var inputPath = parseResult.GetValue(s_pathArgument) ?? Directory.GetCurrentDirectory();
        inputPath = Path.GetFullPath(inputPath);
        var outputBase = Path.GetFullPath(parseResult.GetValue(s_outputOption)!);
        var templateName = parseResult.GetValue(s_nameOption);
        var dryRun = parseResult.GetValue(s_dryRunOption);
        var jsonOutput = parseResult.GetValue(s_jsonOption);

        // Resolve the template directory and manifest
        var (templateDir, manifest) = await ResolveTemplateAsync(inputPath, templateName, cancellationToken);
        if (manifest is null)
        {
            return ExitCodeConstants.InvalidCommand;
        }

        // Generate the variable matrix
        var (matrixVars, matrix) = GenerateMatrix(manifest);

        if (!jsonOutput)
        {
            var emoji = dryRun ? KnownEmojis.MagnifyingGlassTiltedLeft : KnownEmojis.Microscope;
            var action = dryRun ? "Previewing" : "Testing";
            InteractionService.DisplayMessage(emoji, $"{action} template '{manifest.Name}' ({matrix.Count} combinations)");
            InteractionService.DisplayPlainText("");
        }

        if (dryRun)
        {
            return RenderDryRun(matrixVars, matrix, jsonOutput);
        }

        // Create output directory
        Directory.CreateDirectory(outputBase);

        // Execute each combination
        var results = new List<TestResult>();
        for (var i = 0; i < matrix.Count; i++)
        {
            var combo = matrix[i];
            var index = i + 1;
            var dirName = BuildDirectoryName(index, matrixVars, combo);
            var outputDir = Path.Combine(outputBase, dirName);
            var projectName = $"TestProject{index:D3}";

            var variables = new Dictionary<string, string> { ["projectName"] = projectName };
            for (var v = 0; v < matrixVars.Count; v++)
            {
                variables[matrixVars[v]] = combo[v];
            }

            string? error = null;
            try
            {
                await _engine.ApplyAsync(templateDir, outputDir, variables, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            results.Add(new TestResult(index, variables, outputDir, error));

            if (!jsonOutput)
            {
                RenderResultLine(index, matrixVars, combo, outputDir, error);
            }
        }

        // Summary
        var passed = results.Count(r => r.Error is null);
        var failed = results.Count - passed;

        if (jsonOutput)
        {
            RenderJsonOutput(manifest.Name, results, passed, failed);
        }
        else
        {
            InteractionService.DisplayPlainText("");
            if (failed == 0)
            {
                InteractionService.DisplaySuccess($"All {passed} combinations passed");
            }
            else
            {
                InteractionService.DisplayError($"{failed} of {results.Count} combinations failed");
            }
            InteractionService.DisplayPlainText($"Output: {outputBase}");
        }

        return failed > 0 ? 1 : 0;
    }

    private async Task<(string templateDir, GitTemplateManifest? manifest)> ResolveTemplateAsync(
        string inputPath, string? templateName, CancellationToken cancellationToken)
    {
        // Check for direct aspire-template.json
        var manifestPath = Path.Combine(inputPath, "aspire-template.json");
        if (File.Exists(manifestPath))
        {
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
            var manifest = JsonSerializer.Deserialize(json, GitTemplateJsonContext.Default.GitTemplateManifest);
            if (manifest is null)
            {
                InteractionService.DisplayError("Failed to parse aspire-template.json");
                return (inputPath, null);
            }
            return (inputPath, manifest);
        }

        // Check for aspire-template-index.json
        var indexPath = Path.Combine(inputPath, "aspire-template-index.json");
        if (File.Exists(indexPath))
        {
            var indexJson = await File.ReadAllTextAsync(indexPath, cancellationToken).ConfigureAwait(false);
            var index = JsonSerializer.Deserialize(indexJson, GitTemplateJsonContext.Default.GitTemplateIndex);
            if (index?.Templates is null or { Count: 0 })
            {
                InteractionService.DisplayError("No templates found in aspire-template-index.json");
                return (inputPath, null);
            }

            GitTemplateIndexEntry? entry;
            if (templateName is not null)
            {
                entry = index.Templates.FirstOrDefault(t => string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                {
                    InteractionService.DisplayError($"Template '{templateName}' not found in index. Available: {string.Join(", ", index.Templates.Select(t => t.Name))}");
                    return (inputPath, null);
                }
            }
            else if (index.Templates.Count == 1)
            {
                entry = index.Templates[0];
            }
            else
            {
                entry = await InteractionService.PromptForSelectionAsync(
                    "Select a template to test",
                    index.Templates,
                    t => t.Name,
                    cancellationToken);
            }

            var templateDir = Path.GetFullPath(Path.Combine(inputPath, entry.Path));
            var templateManifestPath = Path.Combine(templateDir, "aspire-template.json");
            if (!File.Exists(templateManifestPath))
            {
                InteractionService.DisplayError($"aspire-template.json not found at {templateDir}");
                return (templateDir, null);
            }

            var tmplJson = await File.ReadAllTextAsync(templateManifestPath, cancellationToken).ConfigureAwait(false);
            var tmplManifest = JsonSerializer.Deserialize(tmplJson, GitTemplateJsonContext.Default.GitTemplateManifest);
            return (templateDir, tmplManifest);
        }

        InteractionService.DisplayError($"No aspire-template.json or aspire-template-index.json found in {inputPath}");
        return (inputPath, null);
    }

    private static (List<string> variableNames, List<string[]> matrix) GenerateMatrix(GitTemplateManifest manifest)
    {
        var varNames = new List<string>();
        var valueSets = new List<List<string>>();

        if (manifest.Variables is null)
        {
            return (varNames, [[]]);
        }

        foreach (var (name, varDef) in manifest.Variables)
        {
            // Skip projectName — it gets a unique generated value per combination
            if (string.Equals(name, "projectName", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var testValues = GetTestValues(varDef);
            if (testValues.Count > 0)
            {
                varNames.Add(name);
                valueSets.Add(testValues);
            }
        }

        // Compute cartesian product
        var matrix = CartesianProduct(valueSets);
        return (varNames, matrix);
    }

    private static List<string> GetTestValues(GitTemplateVariable varDef)
    {
        // Use explicit testValues if provided
        if (varDef.TestValues is { Count: > 0 })
        {
            return varDef.TestValues.Select(v => v?.ToString() ?? "").ToList();
        }

        // Infer from type
        return varDef.Type.ToLowerInvariant() switch
        {
            "boolean" => ["true", "false"],
            "choice" when varDef.Choices is { Count: > 0 } => varDef.Choices.Select(c => c.Value).ToList(),
            "integer" => GetIntegerTestValues(varDef),
            _ => [varDef.DefaultValue?.ToString() ?? "TestValue"]
        };
    }

    private static List<string> GetIntegerTestValues(GitTemplateVariable varDef)
    {
        var values = new HashSet<string>();

        if (varDef.DefaultValue is not null)
        {
            values.Add(varDef.DefaultValue.ToString()!);
        }
        if (varDef.Validation?.Min is { } min)
        {
            values.Add(min.ToString(CultureInfo.InvariantCulture));
        }
        if (varDef.Validation?.Max is { } max)
        {
            values.Add(max.ToString(CultureInfo.InvariantCulture));
        }

        return values.Count > 0 ? [.. values] : [varDef.DefaultValue?.ToString() ?? "0"];
    }

    private static List<string[]> CartesianProduct(List<List<string>> sets)
    {
        if (sets.Count == 0)
        {
            return [[]];
        }

        var result = new List<string[]> { Array.Empty<string>() };

        foreach (var set in sets)
        {
            var newResult = new List<string[]>();
            foreach (var existing in result)
            {
                foreach (var value in set)
                {
                    var combined = new string[existing.Length + 1];
                    existing.CopyTo(combined, 0);
                    combined[existing.Length] = value;
                    newResult.Add(combined);
                }
            }
            result = newResult;
        }

        return result;
    }

    private static string BuildDirectoryName(int index, List<string> varNames, string[] values)
    {
        var sb = new StringBuilder();
        sb.Append(index.ToString("D3", CultureInfo.InvariantCulture));
        for (var i = 0; i < varNames.Count; i++)
        {
            sb.Append('_');
            sb.Append(varNames[i]);
            sb.Append('-');
            sb.Append(values[i]);
        }
        return sb.ToString();
    }

    private void RenderResultLine(int index, List<string> varNames, string[] values, string outputDir, string? error)
    {
        var varSummary = new StringBuilder();
        for (var i = 0; i < varNames.Count; i++)
        {
            if (i > 0)
            {
                varSummary.Append("  ");
            }
            varSummary.Append(CultureInfo.InvariantCulture, $"{varNames[i]}={values[i]}");
        }

        if (error is null)
        {
            InteractionService.DisplayMessage(KnownEmojis.CheckMark, $"#{index:D3}  {varSummary}");
            InteractionService.DisplayPlainText($"      → {outputDir}");
        }
        else
        {
            InteractionService.DisplayError($"#{index:D3}  {varSummary}");
            InteractionService.DisplayPlainText($"      → {outputDir}");
            InteractionService.DisplayPlainText($"      Error: {error}");
        }
    }

    private int RenderDryRun(List<string> varNames, List<string[]> matrix, bool jsonOutput)
    {
        if (jsonOutput)
        {
            var combinations = new List<TemplateTestDryRunCombination>();
            for (var i = 0; i < matrix.Count; i++)
            {
                var combo = matrix[i];
                var vars = new Dictionary<string, string>();
                for (var v = 0; v < varNames.Count; v++)
                {
                    vars[varNames[v]] = combo[v];
                }
                combinations.Add(new TemplateTestDryRunCombination { Index = i + 1, Variables = vars });
            }

            var dryRunResult = new TemplateTestDryRunResult { TotalCombinations = matrix.Count, Combinations = combinations };
            var json = JsonSerializer.Serialize(dryRunResult, TemplateTestJsonContext.Default.TemplateTestDryRunResult);
            InteractionService.DisplayPlainText(json);
        }
        else
        {
            for (var i = 0; i < matrix.Count; i++)
            {
                var combo = matrix[i];
                var varSummary = new StringBuilder();
                for (var v = 0; v < varNames.Count; v++)
                {
                    if (v > 0)
                    {
                        varSummary.Append("  ");
                    }
                    varSummary.Append(CultureInfo.InvariantCulture, $"{varNames[v]}={combo[v]}");
                }
                InteractionService.DisplayPlainText($"  #{i + 1:D3}  {varSummary}");
            }
            InteractionService.DisplayPlainText("");
            InteractionService.DisplayPlainText($"Total: {matrix.Count} combinations");
        }
        return 0;
    }

    private void RenderJsonOutput(string templateName, List<TestResult> results, int passed, int failed)
    {
        var output = new TemplateTestRunResult
        {
            Template = templateName,
            TotalCombinations = results.Count,
            Passed = passed,
            Failed = failed,
            Results = results.Select(r => new TemplateTestRunResultEntry
            {
                Index = r.Index,
                Status = r.Error is null ? "passed" : "failed",
                Variables = r.Variables.Where(kv => !string.Equals(kv.Key, "projectName", StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(kv => kv.Key, kv => kv.Value),
                OutputPath = r.OutputPath,
                Error = r.Error
            }).ToList()
        };

        var json = JsonSerializer.Serialize(output, TemplateTestJsonContext.Default.TemplateTestRunResult);
        InteractionService.DisplayPlainText(json);
    }

    private sealed record TestResult(int Index, Dictionary<string, string> Variables, string OutputPath, string? Error);
}

internal sealed class TemplateTestDryRunCombination
{
    public int Index { get; set; }
    public Dictionary<string, string> Variables { get; set; } = [];
}

internal sealed class TemplateTestDryRunResult
{
    public int TotalCombinations { get; set; }
    public List<TemplateTestDryRunCombination> Combinations { get; set; } = [];
}

internal sealed class TemplateTestRunResultEntry
{
    public int Index { get; set; }
    public string Status { get; set; } = "";
    public Dictionary<string, string> Variables { get; set; } = [];
    public string OutputPath { get; set; } = "";
    public string? Error { get; set; }
}

internal sealed class TemplateTestRunResult
{
    public string Template { get; set; } = "";
    public int TotalCombinations { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public List<TemplateTestRunResultEntry> Results { get; set; } = [];
}

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TemplateTestDryRunResult))]
[JsonSerializable(typeof(TemplateTestRunResult))]
internal sealed partial class TemplateTestJsonContext : JsonSerializerContext
{
}
