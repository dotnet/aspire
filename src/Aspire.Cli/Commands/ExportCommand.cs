// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Configuration;
using Aspire.Cli.Interaction;
using Aspire.Cli.Otlp;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Aspire.Cli.Utils;
using Aspire.Dashboard.Utils;
using Aspire.Otlp.Serialization;
using Aspire.Shared.Export;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Command to export telemetry and resource data to a zip file.
/// </summary>
internal sealed class ExportCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.Monitoring;

    private readonly IInteractionService _interactionService;
    private readonly AppHostConnectionResolver _connectionResolver;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger<ExportCommand> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly OptionWithLegacy<FileInfo?> s_appHostOption = new("--apphost", "--project", SharedCommandStrings.AppHostOptionDescription);

    private static readonly Option<string?> s_outputOption = new("--output", "-o")
    {
        Description = ExportCommandStrings.OutputOptionDescription
    };

    private static readonly Argument<string?> s_resourceArgument = new("resource")
    {
        Description = ExportCommandStrings.ResourceOptionDescription,
        Arity = ArgumentArity.ZeroOrOne
    };

    public ExportCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        IHttpClientFactory httpClientFactory,
        ILogger<ExportCommand> logger)
        : base("export", ExportCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _backchannelMonitor = backchannelMonitor;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _connectionResolver = new AppHostConnectionResolver(backchannelMonitor, interactionService, executionContext, logger);

        Arguments.Add(s_resourceArgument);
        Options.Add(s_appHostOption);
        Options.Add(s_outputOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var resourceName = parseResult.GetValue(s_resourceArgument);
        var passedAppHostProjectFile = parseResult.GetValue(s_appHostOption);
        var outputPath = parseResult.GetValue(s_outputOption);

        // Resolve the AppHost connection for backchannel access
        var connectionResult = await _connectionResolver.ResolveConnectionAsync(
            passedAppHostProjectFile,
            SharedCommandStrings.ScanningForRunningAppHosts,
            string.Format(CultureInfo.CurrentCulture, SharedCommandStrings.SelectAppHost, ExportCommandStrings.SelectAppHostAction),
            SharedCommandStrings.AppHostNotRunning,
            cancellationToken);

        if (!connectionResult.Success)
        {
            _interactionService.DisplayMessage(KnownEmojis.Information, connectionResult.ErrorMessage);
            return ExitCodeConstants.Success;
        }

        var connection = connectionResult.Connection!;

        // Get dashboard API info for telemetry data
        var dashboardInfo = await connection.GetDashboardInfoV2Async(cancellationToken);
        if (dashboardInfo?.ApiBaseUrl is null || dashboardInfo.ApiToken is null)
        {
            _interactionService.DisplayError(TelemetryCommandStrings.DashboardApiNotAvailable);
            return ExitCodeConstants.DashboardFailure;
        }

        var baseUrl = dashboardInfo.ApiBaseUrl;
        var apiToken = dashboardInfo.ApiToken;

        // Default file name if not specified
        if (string.IsNullOrEmpty(outputPath))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            outputPath = $"aspire-export-{timestamp}.zip";
        }

        // Ensure directory exists
        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            using var client = TelemetryCommandHelpers.CreateApiClient(_httpClientFactory, apiToken);

            // Get telemetry resources from the Dashboard API
            var telemetryResources = await TelemetryCommandHelpers.GetAllResourcesAsync(client, baseUrl, cancellationToken).ConfigureAwait(false);

            // Get resource snapshots from the backchannel
            var snapshots = await connection.GetResourceSnapshotsAsync(cancellationToken).ConfigureAwait(false);

            // Filter by resource name if specified
            if (resourceName is not null)
            {
                snapshots = snapshots.Where(s =>
                    string.Equals(s.Name, resourceName, StringComparisons.ResourceName) ||
                    string.Equals(s.DisplayName, resourceName, StringComparisons.ResourceName)).ToList();

                if (snapshots.Count == 0)
                {
                    _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ExportCommandStrings.ResourceNotFound, resourceName));
                    return ExitCodeConstants.InvalidCommand;
                }
            }

            if (snapshots.Count == 0)
            {
                _interactionService.DisplayMessage(KnownEmojis.Information, ExportCommandStrings.NoResourcesFound);
                return ExitCodeConstants.Success;
            }

            // Resolve which telemetry resources match the filter
            List<string>? resolvedTelemetryResources = null;
            if (resourceName is not null)
            {
                TelemetryCommandHelpers.TryResolveResourceNames(resourceName, telemetryResources, out resolvedTelemetryResources);
            }

            using var fileStream = File.Create(outputPath);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false);

            // 1. Export resource details
            await _interactionService.ShowStatusAsync(ExportCommandStrings.GatheringResources, () =>
            {
                ExportResources(archive, snapshots);
                return Task.FromResult(true);
            });

            // 2. Export console logs from backchannel
            await _interactionService.ShowStatusAsync(ExportCommandStrings.GatheringConsoleLogs, async () =>
            {
                await ExportConsoleLogsAsync(archive, connection, snapshots, cancellationToken).ConfigureAwait(false);
                return true;
            });

            // 3. Export structured logs from Dashboard API
            await _interactionService.ShowStatusAsync(ExportCommandStrings.GatheringStructuredLogs, async () =>
            {
                await ExportStructuredLogsAsync(archive, client, baseUrl, resolvedTelemetryResources, cancellationToken).ConfigureAwait(false);
                return true;
            });

            // 4. Export traces from Dashboard API
            await _interactionService.ShowStatusAsync(ExportCommandStrings.GatheringTraces, async () =>
            {
                await ExportTracesAsync(archive, client, baseUrl, resolvedTelemetryResources, cancellationToken).ConfigureAwait(false);
                return true;
            });

            var fullPath = Path.GetFullPath(outputPath);
            _interactionService.DisplayMessage(KnownEmojis.CheckMark, string.Format(CultureInfo.CurrentCulture, ExportCommandStrings.ExportComplete, fullPath));
            return ExitCodeConstants.Success;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch telemetry data during export");
            _interactionService.DisplayError(string.Format(CultureInfo.CurrentCulture, ExportCommandStrings.FailedToFetchTelemetry, ex.Message));
            return ExitCodeConstants.DashboardFailure;
        }
    }

    private static void ExportResources(ZipArchive archive, IReadOnlyList<ResourceSnapshot> snapshots)
    {
        var resourceJsonList = ResourceSnapshotMapper.MapToResourceJsonList(snapshots);

        foreach (var (snapshot, resourceJson) in snapshots.Zip(resourceJsonList))
        {
            var displayName = ResourceSnapshotMapper.GetResourceName(snapshot, snapshots);
            var json = JsonSerializer.Serialize(resourceJson, ResourcesCommandJsonContext.RelaxedEscaping.ResourceJson);
            TelemetryArchiveWriter.WriteTextToArchive(archive, $"resources/{TelemetryArchiveWriter.SanitizeFileName(displayName)}.json", json);
        }
    }

    private static async Task ExportConsoleLogsAsync(
        ZipArchive archive,
        IAppHostAuxiliaryBackchannel connection,
        IReadOnlyList<ResourceSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        foreach (var snapshot in snapshots)
        {
            var logLines = new List<string>();

            await foreach (var logLine in connection.GetResourceLogsAsync(snapshot.Name, follow: false, cancellationToken).ConfigureAwait(false))
            {
                logLines.Add(logLine.Content);
            }

            if (logLines.Count > 0)
            {
                var displayName = ResourceSnapshotMapper.GetResourceName(snapshot, snapshots);
                TelemetryArchiveWriter.WriteConsoleLogsToArchive(archive, displayName, logLines);
            }
        }
    }

    private static async Task ExportStructuredLogsAsync(
        ZipArchive archive,
        HttpClient client,
        string baseUrl,
        List<string>? resolvedResources,
        CancellationToken cancellationToken)
    {
        var url = DashboardUrls.TelemetryLogsApiUrl(baseUrl, resolvedResources);
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var apiResponse = JsonSerializer.Deserialize(json, OtlpCliJsonSerializerContext.Default.TelemetryApiResponse);

        if (apiResponse?.Data?.ResourceLogs is { Length: > 0 })
        {
            var data = new OtlpTelemetryDataJson
            {
                ResourceLogs = apiResponse.Data.ResourceLogs
            };
            TelemetryArchiveWriter.WriteJsonToArchive(archive, "structuredlogs/structured-logs.json", data, OtlpCliJsonSerializerContext.Default.OtlpTelemetryDataJson);
        }
    }

    private static async Task ExportTracesAsync(
        ZipArchive archive,
        HttpClient client,
        string baseUrl,
        List<string>? resolvedResources,
        CancellationToken cancellationToken)
    {
        var url = DashboardUrls.TelemetryTracesApiUrl(baseUrl, resolvedResources);
        var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var apiResponse = JsonSerializer.Deserialize(json, OtlpCliJsonSerializerContext.Default.TelemetryApiResponse);

        if (apiResponse?.Data?.ResourceSpans is { Length: > 0 })
        {
            var data = new OtlpTelemetryDataJson
            {
                ResourceSpans = apiResponse.Data.ResourceSpans
            };
            TelemetryArchiveWriter.WriteJsonToArchive(archive, "traces/traces.json", data, OtlpCliJsonSerializerContext.Default.OtlpTelemetryDataJson);
        }
    }
}
