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
using Aspire.Cli.Utils;
using Aspire.Shared.Model.Serialization;
using Microsoft.Extensions.Logging;

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
[JsonSerializable(typeof(ResourceEnvironmentVariableJson))]
[JsonSerializable(typeof(ResourceHealthReportJson))]
[JsonSerializable(typeof(ResourcePropertyJson))]
[JsonSerializable(typeof(ResourceRelationshipJson))]
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

internal sealed class ResourcesCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;

    public ResourcesCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<ResourcesCommand> logger)
        : base("resources", ResourcesCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(backchannelMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _interactionService = interactionService;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);

        var resourceArgument = new Argument<string?>("resource");
        resourceArgument.Description = ResourcesCommandStrings.ResourceArgumentDescription;
        resourceArgument.Arity = ArgumentArity.ZeroOrOne;
        Arguments.Add(resourceArgument);

        var projectOption = new Option<FileInfo?>("--project");
        projectOption.Description = ResourcesCommandStrings.ProjectOptionDescription;
        Options.Add(projectOption);

        var watchOption = new Option<bool>("--watch");
        watchOption.Description = ResourcesCommandStrings.WatchOptionDescription;
        Options.Add(watchOption);

        var formatOption = new Option<OutputFormat>("--format")
        {
            Description = ResourcesCommandStrings.JsonOptionDescription
        };
        Options.Add(formatOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var resourceName = parseResult.GetValue<string?>("resource");
        var passedAppHostProjectFile = parseResult.GetValue<FileInfo?>("--project");
        var watch = parseResult.GetValue<bool>("--watch");
        var format = parseResult.GetValue<OutputFormat>("--format");

        // When outputting JSON, suppress status messages to keep output machine-readable
        var scanningMessage = format == OutputFormat.Json ? string.Empty : ResourcesCommandStrings.ScanningForRunningAppHosts;

        var result = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            scanningMessage,
            ResourcesCommandStrings.SelectAppHost,
            ResourcesCommandStrings.NoInScopeAppHostsShowingAll,
            ResourcesCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!result.Success)
        {
            // No running AppHosts is not an error - similar to Unix 'ps' returning empty
            return ExitCodeConstants.Success;
        }

        if (watch)
        {
            return await ExecuteWatchAsync(result.Connection!, resourceName, format, cancellationToken);
        }
        else
        {
            return await ExecuteSnapshotAsync(result.Connection!, resourceName, format, cancellationToken);
        }
    }

    private async Task<int> ExecuteSnapshotAsync(AppHostAuxiliaryBackchannel connection, string? resourceName, OutputFormat format, CancellationToken cancellationToken)
    {
        // Get current resource snapshots using the dedicated RPC method
        var snapshots = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);

        // Filter by resource name if specified
        if (resourceName is not null)
        {
            snapshots = snapshots.Where(s => string.Equals(s.Name, resourceName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Check if resource was not found
        if (resourceName is not null && snapshots.Count == 0)
        {
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ResourcesCommandStrings.ResourceNotFound, resourceName));
            return ExitCodeConstants.FailedToFindProject;
        }

        var resourceList = snapshots.Select(MapToResourceJson).ToList();

        if (format == OutputFormat.Json)
        {
            var output = new ResourcesOutput { Resources = resourceList.ToArray() };
            var json = JsonSerializer.Serialize(output, ResourcesCommandJsonContext.RelaxedEscaping.ResourcesOutput);
            _interactionService.DisplayRawText(json);
        }
        else
        {
            DisplayResourcesTable(resourceList);
        }

        return ExitCodeConstants.Success;
    }

    private async Task<int> ExecuteWatchAsync(AppHostAuxiliaryBackchannel connection, string? resourceName, OutputFormat format, CancellationToken cancellationToken)
    {
        // Stream resource snapshots
        await foreach (var snapshot in connection.WatchResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false))
        {
            // Filter by resource name if specified
            if (resourceName is not null && !string.Equals(snapshot.Name, resourceName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var resourceJson = MapToResourceJson(snapshot);

            if (format == OutputFormat.Json)
            {
                // NDJSON output - compact, one object per line for streaming
                var json = JsonSerializer.Serialize(resourceJson, ResourcesCommandJsonContext.Ndjson.ResourceJson);
                _interactionService.DisplayRawText(json);
            }
            else
            {
                // Human-readable update
                DisplayResourceUpdate(resourceJson);
            }
        }

        return ExitCodeConstants.Success;
    }

    private void DisplayResourcesTable(List<ResourceJson> resources)
    {
        if (resources.Count == 0)
        {
            _interactionService.DisplayPlainText("No resources found.");
            return;
        }

        // Calculate column widths based on data
        var nameWidth = Math.Max("NAME".Length, resources.Max(r => r.Name?.Length ?? 0));
        var typeWidth = Math.Max("TYPE".Length, resources.Max(r => r.ResourceType?.Length ?? 0));
        var stateWidth = Math.Max("STATE".Length, resources.Max(r => r.State?.Length ?? "Unknown".Length));
        var healthWidth = Math.Max("HEALTH".Length, resources.Max(r => r.HealthStatus?.Length ?? 1));

        var totalWidth = nameWidth + typeWidth + stateWidth + healthWidth + 12 + 20; // 12 for spacing, 20 for endpoints min

        // Header
        _interactionService.DisplayPlainText("");
        _interactionService.DisplayPlainText($"{"NAME".PadRight(nameWidth)}  {"TYPE".PadRight(typeWidth)}  {"STATE".PadRight(stateWidth)}  {"HEALTH".PadRight(healthWidth)}  {"ENDPOINTS"}");
        _interactionService.DisplayPlainText(new string('-', totalWidth));

        foreach (var resource in resources.OrderBy(r => r.Name))
        {
            var endpoints = resource.Urls?.Length > 0
                ? string.Join(", ", resource.Urls.Where(u => !u.IsInternal).Select(u => u.Url))
                : "-";

            var name = resource.Name ?? "-";
            var type = resource.ResourceType ?? "-";
            var state = resource.State ?? "Unknown";
            var health = resource.HealthStatus ?? "-";

            _interactionService.DisplayPlainText($"{name.PadRight(nameWidth)}  {type.PadRight(typeWidth)}  {state.PadRight(stateWidth)}  {health.PadRight(healthWidth)}  {endpoints}");
        }

        _interactionService.DisplayPlainText("");
    }

    private void DisplayResourceUpdate(ResourceJson resource)
    {
        var endpoints = resource.Urls?.Length > 0
            ? string.Join(", ", resource.Urls.Where(u => !u.IsInternal).Select(u => u.Url))
            : "";

        var health = !string.IsNullOrEmpty(resource.HealthStatus) ? $" ({resource.HealthStatus})" : "";
        var endpointsStr = !string.IsNullOrEmpty(endpoints) ? $" - {endpoints}" : "";

        _interactionService.DisplayPlainText($"[{resource.Name}] {resource.State ?? "Unknown"}{health}{endpointsStr}");
    }

    private static ResourceJson MapToResourceJson(ResourceSnapshot snapshot)
    {
        return new ResourceJson
        {
            Name = snapshot.Name,
            DisplayName = snapshot.Name, // Use name as display name for now
            ResourceType = snapshot.Type,
            State = snapshot.State,
            StateStyle = snapshot.StateStyle,
            CreationTimestamp = snapshot.CreatedAt,
            StartTimestamp = snapshot.StartedAt,
            StopTimestamp = snapshot.StoppedAt,
            ExitCode = snapshot.ExitCode,
            HealthStatus = snapshot.HealthStatus,
            Urls = snapshot.Endpoints is { Length: > 0 }
                ? snapshot.Endpoints.Select(e => new ResourceUrlJson
                {
                    Name = e.Name,
                    Url = e.Url,
                    IsInternal = e.IsInternal
                }).ToArray()
                : null,
            Volumes = snapshot.Volumes is { Length: > 0 }
                ? snapshot.Volumes.Select(v => new ResourceVolumeJson
                {
                    Source = v.Source,
                    Target = v.Target,
                    MountType = v.MountType,
                    IsReadOnly = v.IsReadOnly
                }).ToArray()
                : null,
            HealthReports = snapshot.HealthReports is { Length: > 0 }
                ? snapshot.HealthReports.Select(h => new ResourceHealthReportJson
                {
                    Name = h.Name,
                    Status = h.Status,
                    Description = h.Description,
                    ExceptionMessage = h.ExceptionText
                }).ToArray()
                : null,
            Properties = snapshot.Properties is { Count: > 0 }
                ? snapshot.Properties.Select(p => new ResourcePropertyJson
                {
                    Name = p.Key,
                    Value = p.Value
                }).ToArray()
                : null,
            Relationships = snapshot.Relationships is { Length: > 0 }
                ? snapshot.Relationships.Select(r => new ResourceRelationshipJson
                {
                    Type = r.Type,
                    ResourceName = r.ResourceName
                }).ToArray()
                : null
        };
    }
}
