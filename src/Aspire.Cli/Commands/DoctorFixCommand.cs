// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using System.Text.Json;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Cli.Utils.EnvironmentChecker;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Sub-command of <c>aspire doctor</c> that fixes identified environment issues.
/// Dynamically creates sub-commands for each registered <see cref="IHealableEnvironmentCheck"/>.
/// </summary>
internal sealed class DoctorFixCommand : BaseCommand
{
    private readonly IHealableEnvironmentCheck[] _healableChecks;
    private readonly IAnsiConsole _ansiConsole;

    private static readonly Option<bool> s_allOption = new("--all")
    {
        Description = DoctorFixCommandStrings.AllOptionDescription
    };

    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = DoctorFixCommandStrings.JsonOptionDescription
    };

    public DoctorFixCommand(
        IEnumerable<IHealableEnvironmentCheck> healableChecks,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        IInteractionService interactionService,
        IAnsiConsole ansiConsole,
        AspireCliTelemetry telemetry)
        : base("fix", DoctorFixCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _healableChecks = healableChecks.ToArray();
        _ansiConsole = ansiConsole;

        Options.Add(s_allOption);
        Options.Add(s_formatOption);

        foreach (var check in _healableChecks)
        {
            Subcommands.Add(CreateCheckCommand(check));
        }
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var fixAll = parseResult.GetValue(s_allOption);
        var format = parseResult.GetValue(s_formatOption);

        if (!fixAll)
        {
            new HelpAction().Invoke(parseResult);
            return ExitCodeConstants.InvalidCommand;
        }

        return await ExecuteFixAllAsync(format, cancellationToken);
    }

    /// <summary>
    /// Creates a sub-command for a healable check (e.g., <c>aspire doctor fix certificates</c>).
    /// </summary>
    private Command CreateCheckCommand(IHealableEnvironmentCheck check)
    {
        var command = new Command(check.HealCommandName, check.HealCommandDescription);
        command.Options.Add(s_formatOption);

        command.SetAction(async (parseResult, cancellationToken) =>
        {
            var format = parseResult.GetValue(s_formatOption);
            RouteOutputForFormat(format);
            return await ExecuteCheckFixAsync(check, format, cancellationToken);
        });

        foreach (var action in check.HealActions)
        {
            command.Subcommands.Add(CreateActionCommand(check, action));
        }

        return command;
    }

    /// <summary>
    /// Creates a sub-command for a specific heal action (e.g., <c>aspire doctor fix certificates clean</c>).
    /// </summary>
    private Command CreateActionCommand(IHealableEnvironmentCheck check, HealAction action)
    {
        var actionCommand = new Command(action.Name, action.Description);
        actionCommand.Options.Add(s_formatOption);
        actionCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var format = parseResult.GetValue(s_formatOption);
            RouteOutputForFormat(format);
            return await ExecuteExplicitActionAsync(check, action.Name, format, cancellationToken);
        });
        return actionCommand;
    }

    private async Task<int> ExecuteFixAllAsync(OutputFormat format, CancellationToken cancellationToken)
    {
        var fixResults = new List<DoctorFixActionResult>();

        if (_healableChecks.Length == 0)
        {
            OutputResults(fixResults, format);
            return ExitCodeConstants.Success;
        }

        foreach (var check in _healableChecks)
        {
            var evaluateStatus = string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.EvaluatingCheck, check.HealCommandName);
            var recommendedActions = await InteractionService.ShowStatusAsync(
                evaluateStatus,
                async () => await check.EvaluateAsync(cancellationToken));

            if (recommendedActions.Count == 0)
            {
                continue;
            }

            if (format == OutputFormat.Table)
            {
                var wrenchPrefix = ConsoleHelpers.FormatEmojiPrefix(KnownEmojis.Wrench, _ansiConsole);
                _ansiConsole.MarkupLine($"  [blue]{wrenchPrefix}{string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.ApplyingCategoryFixes, check.HealCommandName).EscapeMarkup()}[/]");
            }

            foreach (var action in recommendedActions)
            {
                var result = await ExecuteHealActionAsync(check, action, format, cancellationToken);
                fixResults.Add(ToFixActionResult(check, action.Name, result));
            }
        }

        OutputResults(fixResults, format);

        var hasFailures = fixResults.Any(r => !r.Success);
        return hasFailures ? ExitCodeConstants.InvalidCommand : ExitCodeConstants.Success;
    }

    private async Task<int> ExecuteCheckFixAsync(IHealableEnvironmentCheck check, OutputFormat format, CancellationToken cancellationToken)
    {
        var fixResults = new List<DoctorFixActionResult>();

        var evaluateStatus = string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.EvaluatingCheck, check.HealCommandName);
        var recommendedActions = await InteractionService.ShowStatusAsync(
            evaluateStatus,
            async () => await check.EvaluateAsync(cancellationToken));

        if (recommendedActions.Count > 0)
        {
            foreach (var action in recommendedActions)
            {
                var result = await ExecuteHealActionAsync(check, action, format, cancellationToken);
                fixResults.Add(ToFixActionResult(check, action.Name, result));
            }
        }

        OutputResults(fixResults, format);

        var hasFailures = fixResults.Any(r => !r.Success);
        return hasFailures ? ExitCodeConstants.InvalidCommand : ExitCodeConstants.Success;
    }

    private async Task<int> ExecuteExplicitActionAsync(IHealableEnvironmentCheck check, string actionName, OutputFormat format, CancellationToken cancellationToken)
    {
        var healAction = check.HealActions.TryGetValue(actionName, out var found) ? found : null;

        var result = await ExecuteHealActionAsync(check, healAction, actionName, format, cancellationToken);
        var fixResult = ToFixActionResult(check, actionName, result);

        OutputResults([fixResult], format);

        return result.Success ? ExitCodeConstants.Success : ExitCodeConstants.InvalidCommand;
    }

    /// <summary>
    /// Executes a heal action without a spinner so that any stdio prompts
    /// (e.g., macOS keychain password dialogs) render cleanly.
    /// </summary>
    private async Task<HealResult> ExecuteHealActionAsync(IHealableEnvironmentCheck check, HealAction? action, OutputFormat format, CancellationToken cancellationToken)
    {
        return await ExecuteHealActionAsync(check, action, action?.Name ?? string.Empty, format, cancellationToken);
    }

    private async Task<HealResult> ExecuteHealActionAsync(IHealableEnvironmentCheck check, HealAction? action, string actionName, OutputFormat format, CancellationToken cancellationToken)
    {
        if (format == OutputFormat.Table)
        {
            var progressDescription = action?.ProgressDescription ?? DoctorFixCommandStrings.FixingIssues;
            var gearPrefix = ConsoleHelpers.FormatEmojiPrefix(KnownEmojis.Gear, _ansiConsole);
            _ansiConsole.MarkupLine($"  [dim]{gearPrefix}{progressDescription.EscapeMarkup()}[/]");
        }

        return await check.HealAsync(actionName, cancellationToken);
    }

    private static DoctorFixActionResult ToFixActionResult(IHealableEnvironmentCheck check, string actionName, HealResult result)
    {
        return new DoctorFixActionResult
        {
            Check = check.HealCommandName,
            Action = actionName,
            Success = result.Success,
            Message = result.Message,
            Details = result.Details
        };
    }

    /// <summary>
    /// Routes human-readable output to stderr when JSON format is requested.
    /// Dynamic sub-commands don't inherit <see cref="BaseCommand"/> routing, so
    /// this must be called explicitly in their action callbacks.
    /// </summary>
    private void RouteOutputForFormat(OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            InteractionService.Console = ConsoleOutput.Error;
        }
    }

    private void OutputResults(List<DoctorFixActionResult> fixResults, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            OutputJson(fixResults);
        }
        else
        {
            OutputHumanReadable(fixResults);
        }
    }

    private void OutputJson(List<DoctorFixActionResult> fixResults)
    {
        var applied = fixResults.Count(r => r.Success);
        var failed = fixResults.Count(r => !r.Success);

        var response = new DoctorFixResponse
        {
            Fixes = fixResults,
            Summary = new DoctorFixSummary
            {
                Applied = applied,
                Failed = failed
            }
        };

        var json = JsonSerializer.Serialize(response, JsonSourceGenerationContext.RelaxedEscaping.DoctorFixResponse);
        InteractionService.DisplayRawText(json, ConsoleOutput.Standard);
    }

    private void OutputHumanReadable(List<DoctorFixActionResult> fixResults)
    {
        if (fixResults.Count == 0)
        {
            var infoPrefix = ConsoleHelpers.FormatEmojiPrefix(KnownEmojis.Information, _ansiConsole);
            _ansiConsole.MarkupLine($"  [blue]{infoPrefix}{DoctorFixCommandStrings.NoFixableIssues.EscapeMarkup()}[/]");
            return;
        }

        _ansiConsole.MarkupLine($"[bold]{DoctorFixCommandStrings.FixResultsHeader}[/]");
        _ansiConsole.WriteLine(new string('=', DoctorFixCommandStrings.FixResultsHeader.Length));
        _ansiConsole.WriteLine();

        // Group results by check
        var groupedResults = fixResults.GroupBy(r => r.Check);

        foreach (var group in groupedResults)
        {
            foreach (var result in group)
            {
                OutputFixActionResult(result);
            }

            _ansiConsole.WriteLine();
        }

        // Output summary
        var applied = fixResults.Count(r => r.Success);
        var failed = fixResults.Count(r => !r.Success);

        _ansiConsole.MarkupLine($"[bold]{string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.SummaryFormat, applied, failed)}[/]");
    }

    private void OutputFixActionResult(DoctorFixActionResult result)
    {
        var (icon, color) = result.Success
            ? (KnownEmojis.CheckMark, "green")
            : (KnownEmojis.CrossMark, "red");
        var iconPrefix = ConsoleHelpers.FormatEmojiPrefix(icon, _ansiConsole);

        var message = result.Success
            ? string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.FixApplied, result.Message)
            : string.Format(CultureInfo.CurrentCulture, DoctorFixCommandStrings.FixFailed, result.Message);

        // Primary grid: icon + message (wrapped lines stay aligned with message text)
        var messageGrid = new Grid();
        messageGrid.AddColumn();
        messageGrid.AddRow(new Markup($"[{color}]{iconPrefix}{message.EscapeMarkup()}[/]"));

        _ansiConsole.Write(new Padder(messageGrid, new Padding(2, 0)));

        if (!string.IsNullOrEmpty(result.Details))
        {
            var detailGrid = new Grid();
            detailGrid.AddColumn();
            detailGrid.AddRow(new Markup($"[dim]{result.Details.EscapeMarkup()}[/]"));

            _ansiConsole.Write(new Padder(detailGrid, new Padding(7, 0)));
        }
    }
}
