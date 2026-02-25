// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Shared.Model.Serialization;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Aspire.Cli.Commands;

/// <summary>
/// Output format for resources command (array wrapper).
/// </summary>
internal sealed class ResourcesOutput
{
    public required ResourceJson[] Resources { get; init; }
}

[JsonSerializable(typeof(ResourcesOutput))]
[JsonSerializable(typeof(ResourceJson))]
[JsonSerializable(typeof(ResourceUrlJson))]
[JsonSerializable(typeof(ResourceVolumeJson))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(Dictionary<string, ResourceHealthReportJson>))]
[JsonSerializable(typeof(ResourceRelationshipJson))]
[JsonSerializable(typeof(Dictionary<string, ResourceCommandJson>))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class ResourcesCommandJsonContext : JsonSerializerContext
{
    private static ResourcesCommandJsonContext? s_relaxedEscaping;
    private static ResourcesCommandJsonContext? s_ndjson;

    /// <summary>
    /// Gets a context with relaxed JSON escaping for non-ASCII character support (pretty-printed).
    /// </summary>
    public static ResourcesCommandJsonContext RelaxedEscaping => s_relaxedEscaping ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });

    /// <summary>
    /// Gets a context for NDJSON streaming (compact, one object per line).
    /// </summary>
    public static ResourcesCommandJsonContext Ndjson => s_ndjson ??= new(new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}

internal sealed class DescribeCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.Monitoring;

    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;

    private static readonly Argument<string?> s_resourceArgument = new("resource")
    {
        Description = DescribeCommandStrings.ResourceArgumentDescription,
        Arity = ArgumentArity.ZeroOrOne
    };
    private static readonly Option<FileInfo?> s_projectOption = new("--project")
    {
        Description = SharedCommandStrings.ProjectOptionDescription
    };
    private static readonly Option<bool> s_followOption = new("--follow", "-f")
    {
        Description = DescribeCommandStrings.FollowOptionDescription
    };
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = DescribeCommandStrings.JsonOptionDescription
    };

    public DescribeCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        ILogger<DescribeCommand> logger)
        : base("describe", DescribeCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        Aliases.Add("resources");
        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);

        Arguments.Add(s_resourceArgument);
        Options.Add(s_projectOption);
        Options.Add(s_followOption);
        Options.Add(s_formatOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var resourceName = parseResult.GetValue(s_resourceArgument);
        var passedAppHostProjectFile = parseResult.GetValue(s_projectOption);
        var follow = parseResult.GetValue(s_followOption);
        var format = parseResult.GetValue(s_formatOption);

        // When outputting JSON, suppress status messages to keep output machine-readable
        var scanningMessage = format == OutputFormat.Json ? string.Empty : SharedCommandStrings.ScanningForRunningAppHosts;

        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            scanningMessage,
            string.Format(CultureInfo.CurrentCulture, SharedCommandStrings.SelectAppHost, DescribeCommandStrings.SelectAppHostAction),
            SharedCommandStrings.NoInScopeAppHostsShowingAll,
            SharedCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!result.Success)
        {
            // No running AppHosts is not an error - similar to Unix 'ps' returning empty
            _interactionService.DisplayMessage("information", result.ErrorMessage);
            return ExitCodeConstants.Success;
        }

        if (follow)
        {
            return await ExecuteWatchAsync(result.Connection!, resourceName, format, cancellationToken);
        }
        else
        {
            return await ExecuteSnapshotAsync(result.Connection!, resourceName, format, cancellationToken);
        }
    }

    private async Task<int> ExecuteSnapshotAsync(IAppHostAuxiliaryBackchannel connection, string? resourceName, OutputFormat format, CancellationToken cancellationToken)
    {
        // Get dashboard URL and resource snapshots in parallel
        var dashboardUrlsTask = connection.GetDashboardUrlsAsync(cancellationToken);
        var snapshotsTask = connection.GetResourceSnapshotsAsync(cancellationToken);

        await Task.WhenAll(dashboardUrlsTask, snapshotsTask).ConfigureAwait(false);

        var dashboardUrls = await dashboardUrlsTask.ConfigureAwait(false);
        var snapshots = await snapshotsTask.ConfigureAwait(false);

        // Filter by resource name if specified
        if (resourceName is not null)
        {
            snapshots = snapshots.Where(s => string.Equals(s.Name, resourceName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Check if resource was not found
        if (resourceName is not null && snapshots.Count == 0)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, DescribeCommandStrings.ResourceNotFound, resourceName));
            return ExitCodeConstants.FailedToFindProject;
        }

        // Use the dashboard base URL if available
        var dashboardBaseUrl = dashboardUrls?.BaseUrlWithLoginToken;
        var resourceList = ResourceSnapshotMapper.MapToResourceJsonList(snapshots, dashboardBaseUrl);

        if (format == OutputFormat.Json)
        {
            var output = new ResourcesOutput { Resources = resourceList.ToArray() };
            var json = JsonSerializer.Serialize(output, ResourcesCommandJsonContext.RelaxedEscaping.ResourcesOutput);
            // Structured output always goes to stdout.
            _interactionService.DisplayRawText(json, ConsoleOutput.Standard);
        }
        else
        {
            DisplayResourcesTable(snapshots);
        }

        return ExitCodeConstants.Success;
    }

    private async Task<int> ExecuteWatchAsync(IAppHostAuxiliaryBackchannel connection, string? resourceName, OutputFormat format, CancellationToken cancellationToken)
    {
        // Get dashboard URL first for generating resource links
        var dashboardUrls = await connection.GetDashboardUrlsAsync(cancellationToken).ConfigureAwait(false);
        var dashboardBaseUrl = dashboardUrls?.BaseUrlWithLoginToken;

        // Maintain a dictionary of all resources seen so far for relationship resolution
        var allResources = new Dictionary<string, ResourceSnapshot>(StringComparer.OrdinalIgnoreCase);

        // Stream resource snapshots
        await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false))
        {
            // Update the dictionary with the latest snapshot for this resource
            allResources[snapshot.Name] = snapshot;

            // Filter by resource name if specified
            if (resourceName is not null && !string.Equals(snapshot.Name, resourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resourceJson = ResourceSnapshotMapper.MapToResourceJson(snapshot, allResources.Values.ToList(), dashboardBaseUrl);

            if (format == OutputFormat.Json)
            {
                // NDJSON output - compact, one object per line for streaming
                var json = JsonSerializer.Serialize(resourceJson, ResourcesCommandJsonContext.Ndjson.ResourceJson);
                // Structured output always goes to stdout.
                _interactionService.DisplayRawText(json, ConsoleOutput.Standard);
            }
            else
            {
                // Human-readable update
                DisplayResourceUpdate(snapshot, allResources);
            }
        }

        return ExitCodeConstants.Success;
    }

    private void DisplayResourcesTable(IReadOnlyList<ResourceSnapshot> snapshots)
    {
        if (snapshots.Count == 0)
        {
            _interactionService.DisplayPlainText("No resources found.");
            return;
        }

        // Get display names for all resources
        var orderedItems = snapshots.Select(s => (Snapshot: s, DisplayName: ResourceSnapshotMapper.GetResourceName(s, snapshots)))
            .OrderBy(x => x.DisplayName)
            .ToList();

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("State");
        table.AddColumn("Health");
        table.AddColumn("Endpoints");

        foreach (var (snapshot, displayName) in orderedItems)
        {
            var endpoints = snapshot.Urls.Length > 0
                ? string.Join(", ", snapshot.Urls.Where(e => !e.IsInternal).Select(e => e.Url))
                : "-";

            var type = snapshot.ResourceType ?? "-";
            var state = snapshot.State ?? "Unknown";
            var health = snapshot.HealthStatus ?? "-";

            // Color the state based on value
            var stateText = state.ToUpperInvariant() switch
            {
                "RUNNING" => $"[green]{state}[/]",
                "FINISHED" or "EXITED" => $"[grey]{state}[/]",
                "FAILEDTOSTART" or "FAILED" => $"[red]{state}[/]",
                "STARTING" or "WAITING" => $"[yellow]{state}[/]",
                _ => state
            };

            // Color the health based on value
            var healthText = health.ToUpperInvariant() switch
            {
                "HEALTHY" => $"[green]{health}[/]",
                "UNHEALTHY" => $"[red]{health}[/]",
                "DEGRADED" => $"[yellow]{health}[/]",
                _ => health
            };

            table.AddRow(displayName, type, stateText, healthText, endpoints);
        }

        AnsiConsole.Write(table);
    }

    private void DisplayResourceUpdate(ResourceSnapshot snapshot, IDictionary<string, ResourceSnapshot> allResources)
    {
        var displayName = ResourceSnapshotMapper.GetResourceName(snapshot, allResources);

        var endpoints = snapshot.Urls.Length > 0
            ? string.Join(", ", snapshot.Urls.Where(e => !e.IsInternal).Select(e => e.Url))
            : "";

        var health = !string.IsNullOrEmpty(snapshot.HealthStatus) ? $" ({snapshot.HealthStatus})" : "";
        var endpointsStr = !string.IsNullOrEmpty(endpoints) ? $" - {endpoints}" : "";

        _interactionService.DisplayPlainText($"[{displayName}] {snapshot.State ?? "Unknown"}{health}{endpointsStr}");
    }
}
