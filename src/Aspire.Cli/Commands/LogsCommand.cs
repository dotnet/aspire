// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// JSON output format for a log line.
/// </summary>
internal sealed class LogLineJson
{
    public required string ResourceName { get; init; }
    public required string Content { get; init; }
    public required bool IsError { get; init; }
}

[JsonSerializable(typeof(LogLineJson))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class LogsCommandJsonContext : JsonSerializerContext
{
    // Use a custom context instance with relaxed escaping for non-ASCII characters
    private static LogsCommandJsonContext? s_relaxedEscaping;

    public static LogsCommandJsonContext RelaxedEscaping => s_relaxedEscaping ??= new LogsCommandJsonContext(
        new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        });
}

internal sealed class LogsCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly ILogger<LogsCommand> _logger;
    private readonly ICliHostEnvironment _hostEnvironment;

    // Colors to cycle through for different resources (similar to docker-compose)
    private static readonly Color[] s_resourceColors =
    [
        Color.Cyan1,
        Color.Green,
        Color.Yellow,
        Color.Blue,
        Color.Magenta1,
        Color.Orange1,
        Color.DeepPink1,
        Color.SpringGreen1,
        Color.Aqua,
        Color.Violet
    ];
    private readonly Dictionary<string, Color> _resourceColorMap = new(StringComparer.OrdinalIgnoreCase);
    private int _nextColorIndex;

    public LogsCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ICliHostEnvironment hostEnvironment,
        ILogger<LogsCommand> logger)
        : base("logs", LogsCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(backchannelMonitor);
        ArgumentNullException.ThrowIfNull(hostEnvironment);
        ArgumentNullException.ThrowIfNull(logger);

        _interactionService = interactionService;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);

        var resourceArgument = new Argument<string?>("resource");
        resourceArgument.Description = LogsCommandStrings.ResourceArgumentDescription;
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = LogsCommandStrings.ProjectOptionDescription;
        Options.Add(projectOption);

        var followOption = new Option<bool>("--follow", "-f");
        followOption.Description = LogsCommandStrings.FollowOptionDescription;
        Options.Add(followOption);

        var formatOption = new Option<OutputFormat>("--format")
        {
            Description = LogsCommandStrings.JsonOptionDescription
        };
        Options.Add(formatOption);

        var tailOption = new Option<int?>("--tail", "-n")
        {
            Description = LogsCommandStrings.TailOptionDescription
        };
        Options.Add(tailOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue<string?>("resource");
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var follow = parseResult.GetValue<bool>("--follow");
        var format = parseResult.GetValue<OutputFormat>("--format");
        var tail = parseResult.GetValue<int?>("--tail");

        // Validate --tail value
        if (tail.HasValue && tail.Value < 1)
        {
            _interactionService.DisplayError(LogsCommandStrings.TailMustBePositive);
            return ExitCodeConstants.InvalidCommand;
        }

        // When outputting JSON, suppress status messages to keep output machine-readable
        var scanningMessage = format == OutputFormat.Json ? string.Empty : LogsCommandStrings.ScanningForRunningAppHosts;

        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            scanningMessage,
            LogsCommandStrings.SelectAppHost,
            LogsCommandStrings.NoInScopeAppHostsShowingAll,
            LogsCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!result.Success)
        {
            // No running AppHosts is not an error - similar to Unix 'ps' returning empty
            return ExitCodeConstants.Success;
        }

        if (follow)
        {
            return await ExecuteWatchAsync(result.Connection!, resourceName, format, tail, cancellationToken);
        }
        else
        {
            return await ExecuteGetAsync(result.Connection!, resourceName, format, tail, cancellationToken);
        }
    }

    private async Task<int> ExecuteGetAsync(
        AppHostAuxiliaryBackchannel connection,
        string? resourceName,
        OutputFormat format,
        int? tail,
        CancellationToken cancellationToken)
    {
        // If tail is not specified, stream directly without buffering
        if (!tail.HasValue)
        {
            await foreach (var logLine in GetLogsAsync(connection, resourceName, cancellationToken).ConfigureAwait(false))
            {
                OutputLogLine(logLine, format);
            }
            return ExitCodeConstants.Success;
        }

        // With tail specified, collect all logs first then output last N
        var logLines = await CollectLogsAsync(connection, resourceName, cancellationToken).ConfigureAwait(false);

        // Apply tail filter (tail.Value is guaranteed >= 1 by earlier validation)
        if (logLines.Count > tail.Value)
        {
            logLines = logLines.Skip(logLines.Count - tail.Value).ToList();
        }

        // Output the logs
        foreach (var logLine in logLines)
        {
            OutputLogLine(logLine, format);
        }

        return ExitCodeConstants.Success;
    }

    private async Task<int> ExecuteWatchAsync(
        AppHostAuxiliaryBackchannel connection,
        string? resourceName,
        OutputFormat format,
        int? tail,
        CancellationToken cancellationToken)
    {
        // If tail is specified, show last N lines first before streaming
        if (tail.HasValue)
        {
            var historicalLogs = await CollectLogsAsync(connection, resourceName, cancellationToken).ConfigureAwait(false);

            // Output last N lines
            var tailedLogs = historicalLogs.Count > tail.Value
                ? historicalLogs.Skip(historicalLogs.Count - tail.Value)
                : historicalLogs;

            foreach (var logLine in tailedLogs)
            {
                OutputLogLine(logLine, format);
            }
        }

        // Now stream new logs
        await foreach (var logLine in connection.GetResourceLogsAsync(resourceName, follow: true, cancellationToken).ConfigureAwait(false))
        {
            OutputLogLine(logLine, format);
        }

        return ExitCodeConstants.Success;
    }

    /// <summary>
    /// Collects all logs for a resource (or all resources if resourceName is null) into a list.
    /// </summary>
    private async Task<List<ResourceLogLine>> CollectLogsAsync(
        AppHostAuxiliaryBackchannel connection,
        string? resourceName,
        CancellationToken cancellationToken)
    {
        var logLines = new List<ResourceLogLine>();
        await foreach (var logLine in GetLogsAsync(connection, resourceName, cancellationToken).ConfigureAwait(false))
        {
            logLines.Add(logLine);
        }
        return logLines;
    }

    /// <summary>
    /// Gets logs for a resource (or all resources if resourceName is null) as an async enumerable.
    /// </summary>
    private async IAsyncEnumerable<ResourceLogLine> GetLogsAsync(
        AppHostAuxiliaryBackchannel connection,
        string? resourceName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (resourceName is not null)
        {
            await foreach (var logLine in connection.GetResourceLogsAsync(resourceName, follow: false, cancellationToken).ConfigureAwait(false))
            {
                yield return logLine;
            }
            yield break;
        }

        // Get all resources and stream logs for each (like docker compose logs)
        var snapshots = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);
        if (snapshots.Count == 0)
        {
            _interactionService.DisplayMessage("ℹ️", LogsCommandStrings.NoResourcesFound);
            yield break;
        }

        foreach (var snapshot in snapshots.OrderBy(s => s.Name))
        {
            await foreach (var logLine in connection.GetResourceLogsAsync(snapshot.Name, follow: false, cancellationToken).ConfigureAwait(false))
            {
                yield return logLine;
            }
        }
    }

    private void OutputLogLine(ResourceLogLine logLine, OutputFormat format)
    {
        if (format == OutputFormat.Json)
        {
            var logLineJson = new LogLineJson
            {
                ResourceName = logLine.ResourceName,
                Content = logLine.Content,
                IsError = logLine.IsError
            };
            var output = JsonSerializer.Serialize(logLineJson, LogsCommandJsonContext.RelaxedEscaping.LogLineJson);
            _interactionService.DisplayRawText(output);
        }
        else if (_hostEnvironment.SupportsAnsi)
        {
            // Colorized output: assign a consistent color to each resource
            var color = GetResourceColor(logLine.ResourceName);
            var escapedContent = logLine.Content.EscapeMarkup();
            AnsiConsole.MarkupLine($"[{color}][[{logLine.ResourceName}]][/] {escapedContent}");
        }
        else
        {
            // Plain text fallback when colors not supported
            _interactionService.DisplayPlainText($"[{logLine.ResourceName}] {logLine.Content}");
        }
    }

    private Color GetResourceColor(string resourceName)
    {
        if (!_resourceColorMap.TryGetValue(resourceName, out var color))
        {
            color = s_resourceColors[_nextColorIndex % s_resourceColors.Length];
            _resourceColorMap[resourceName] = color;
            _nextColorIndex++;
        }
        return color;
    }
}
