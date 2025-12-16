// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Spectre.Console;

namespace Aspire.Cli.Commands;

internal sealed class DoctorCommand : BaseCommand
{
    private readonly IPrerequisiteChecker _prerequisiteChecker;
    private readonly IAnsiConsole _ansiConsole;

    public DoctorCommand(
        IPrerequisiteChecker prerequisiteChecker,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        IAnsiConsole ansiConsole)
        : base("doctor", DoctorCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(prerequisiteChecker);
        ArgumentNullException.ThrowIfNull(ansiConsole);

        _prerequisiteChecker = prerequisiteChecker;
        _ansiConsole = ansiConsole;

        var jsonOption = new Option<bool>("--json");
        jsonOption.Description = DoctorCommandStrings.JsonOptionDescription;
        Options.Add(jsonOption);

        var fixOption = new Option<bool>("--fix");
        fixOption.Description = DoctorCommandStrings.FixOptionDescription;
        Options.Add(fixOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var jsonOutput = parseResult.GetValue<bool>("--json");
        var fix = parseResult.GetValue<bool>("--fix");

        // Run all prerequisite checks
        var results = await InteractionService.ShowStatusAsync(
            DoctorCommandStrings.CheckingPrerequisites,
            async () => await _prerequisiteChecker.CheckAllAsync(cancellationToken));

        if (jsonOutput)
        {
            OutputJson(results);
        }
        else
        {
            OutputHumanReadable(results);
        }

        // Exit code: 0 if no failures (warnings are OK), 1 (InvalidCommand) if any failures
        var hasFailures = results.Any(r => r.Status == PrerequisiteCheckStatus.Fail);
        return hasFailures ? ExitCodeConstants.InvalidCommand : ExitCodeConstants.Success;
    }

    private void OutputJson(IReadOnlyList<PrerequisiteCheckResult> results)
    {
        var passed = results.Count(r => r.Status == PrerequisiteCheckStatus.Pass);
        var warnings = results.Count(r => r.Status == PrerequisiteCheckStatus.Warning);
        var failed = results.Count(r => r.Status == PrerequisiteCheckStatus.Fail);

        // Build JSON manually to avoid AOT warnings
        var checksJson = new System.Text.StringBuilder();
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"{{");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"  \"checks\": [");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            checksJson.AppendLine(CultureInfo.InvariantCulture, $"    {{");
            checksJson.AppendLine(CultureInfo.InvariantCulture, $"      \"category\": \"{EscapeJson(r.Category)}\",");
            checksJson.AppendLine(CultureInfo.InvariantCulture, $"      \"name\": \"{EscapeJson(r.Name)}\",");
            checksJson.AppendLine(CultureInfo.InvariantCulture, $"      \"status\": \"{EscapeJson(r.Status.ToString().ToLowerInvariant())}\",");
            checksJson.AppendLine(CultureInfo.InvariantCulture, $"      \"message\": \"{EscapeJson(r.Message)}\"");
            
            if (!string.IsNullOrEmpty(r.Fix))
            {
                checksJson.AppendLine(CultureInfo.InvariantCulture, $"      ,\"fix\": \"{EscapeJson(r.Fix)}\"");
            }
            
            if (!string.IsNullOrEmpty(r.Link))
            {
                checksJson.AppendLine(CultureInfo.InvariantCulture, $"      ,\"link\": \"{EscapeJson(r.Link)}\"");
            }
            
            if (!string.IsNullOrEmpty(r.Details))
            {
                checksJson.AppendLine(CultureInfo.InvariantCulture, $"      ,\"details\": \"{EscapeJson(r.Details)}\"");
            }

            if (i < results.Count - 1)
            {
                checksJson.AppendLine(CultureInfo.InvariantCulture, $"    }},");
            }
            else
            {
                checksJson.AppendLine(CultureInfo.InvariantCulture, $"    }}");
            }
        }

        checksJson.AppendLine(CultureInfo.InvariantCulture, $"  ],");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"  \"summary\": {{");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"    \"passed\": {passed},");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"    \"warnings\": {warnings},");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"    \"failed\": {failed}");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"  }}");
        checksJson.AppendLine(CultureInfo.InvariantCulture, $"}}");

        _ansiConsole.WriteLine(checksJson.ToString());
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private void OutputHumanReadable(IReadOnlyList<PrerequisiteCheckResult> results)
    {
        _ansiConsole.WriteLine();
        _ansiConsole.MarkupLine($"[bold]{DoctorCommandStrings.EnvironmentCheckHeader}[/]");
        _ansiConsole.WriteLine(new string('=', DoctorCommandStrings.EnvironmentCheckHeader.Length));
        _ansiConsole.WriteLine();

        // Group results by category
        var groupedResults = results
            .GroupBy(r => r.Category)
            .OrderBy(g => GetCategoryOrder(g.Key));

        foreach (var group in groupedResults)
        {
            var categoryHeader = GetCategoryHeader(group.Key);
            _ansiConsole.MarkupLine($"[bold]{categoryHeader}[/]");

            foreach (var result in group)
            {
                OutputCheckResult(result);
            }

            _ansiConsole.WriteLine();
        }

        // Output summary
        var passed = results.Count(r => r.Status == PrerequisiteCheckStatus.Pass);
        var warnings = results.Count(r => r.Status == PrerequisiteCheckStatus.Warning);
        var failed = results.Count(r => r.Status == PrerequisiteCheckStatus.Fail);

        _ansiConsole.MarkupLine($"[bold]{string.Format(CultureInfo.CurrentCulture, DoctorCommandStrings.SummaryFormat, passed, warnings, failed)}[/]");

        // Show link to detailed prerequisites if there are warnings or failures
        if (warnings > 0 || failed > 0)
        {
            _ansiConsole.MarkupLine($"[dim]{DoctorCommandStrings.DetailedPrerequisitesLink}[/]");
        }
    }

    private void OutputCheckResult(PrerequisiteCheckResult result)
    {
        var (icon, color) = GetStatusIconAndColor(result.Status);
        _ansiConsole.MarkupLine($"  [{color}]{icon}[/] {result.Message.EscapeMarkup()}");

        // Show fix suggestion if available
        if (!string.IsNullOrEmpty(result.Fix))
        {
            var fixLines = result.Fix.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in fixLines)
            {
                _ansiConsole.MarkupLine($"       [dim]{line.Trim().EscapeMarkup()}[/]");
            }
        }

        // Show documentation link if available
        if (!string.IsNullOrEmpty(result.Link))
        {
            _ansiConsole.MarkupLine($"       [dim]See: {result.Link.EscapeMarkup()}[/]");
        }
    }

    private static (string Icon, string Color) GetStatusIconAndColor(PrerequisiteCheckStatus status)
    {
        return status switch
        {
            PrerequisiteCheckStatus.Pass => ("[check]", "green"),
            PrerequisiteCheckStatus.Warning => ("[warn]", "yellow"),
            PrerequisiteCheckStatus.Fail => ("[fail]", "red"),
            _ => ("[ ? ]", "grey")
        };
    }

    private static string GetCategoryHeader(string category)
    {
        return category switch
        {
            "sdk" => DoctorCommandStrings.SdkCategoryHeader,
            "container" => DoctorCommandStrings.ContainerCategoryHeader,
            "environment" => DoctorCommandStrings.EnvironmentCategoryHeader,
            _ => category
        };
    }

    private static int GetCategoryOrder(string category)
    {
        return category switch
        {
            "sdk" => 1,
            "container" => 2,
            "environment" => 3,
            _ => 99
        };
    }
}
