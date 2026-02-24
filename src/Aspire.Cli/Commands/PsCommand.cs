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
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Commands;

/// <summary>
/// Represents information about a running AppHost for JSON serialization.
/// Aligned with AppHostListInfo from ListAppHostsTool.
/// </summary>
internal sealed record AppHostDisplayInfo(
    string AppHostPath,
    int AppHostPid,
    int? CliPid,
    string? DashboardUrl);

[JsonSerializable(typeof(List<AppHostDisplayInfo>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class PsCommandJsonContext : JsonSerializerContext
{
    private static PsCommandJsonContext? s_relaxedEscaping;

    /// <summary>
    /// Gets a context with relaxed JSON escaping for non-ASCII character support.
    /// </summary>
    public static PsCommandJsonContext RelaxedEscaping => s_relaxedEscaping ??= new(new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    });
}

internal sealed class PsCommand : BaseCommand
{
    internal override HelpGroup HelpGroup => HelpGroup.AppCommands;

    private readonly IInteractionService _interactionService;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger<PsCommand> _logger;
    private static readonly Option<OutputFormat> s_formatOption = new("--format")
    {
        Description = PsCommandStrings.JsonOptionDescription
    };

    public PsCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        AspireCliTelemetry telemetry,
        ILogger<PsCommand> logger)
        : base("ps", PsCommandStrings.Description, features, updateNotifier, executionContext, interactionService, telemetry)
    {
        _interactionService = interactionService;
        _backchannelMonitor = backchannelMonitor;
        _logger = logger;

        Options.Add(s_formatOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        using var activity = Telemetry.StartDiagnosticActivity(Name);

        var format = parseResult.GetValue(s_formatOption);

        // Scan for running AppHosts (same as ListAppHostsTool)
        // Skip status display for JSON output to avoid contaminating stdout
        List<IAppHostAuxiliaryBackchannel> connections;
        if (format == OutputFormat.Json)
        {
            await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);
            connections = _backchannelMonitor.Connections.ToList();
        }
        else
        {
            connections = await _interactionService.ShowStatusAsync(
                PsCommandStrings.ScanningForRunningAppHosts,
                async () =>
                {
                    await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);
                    return _backchannelMonitor.Connections.ToList();
                });
        }

        if (connections.Count == 0)
        {
            if (format == OutputFormat.Json)
            {
                _interactionService.DisplayPlainText("[]");
            }
            else
            {
                _interactionService.DisplayMessage("information", PsCommandStrings.NoRunningAppHostsFound);
            }
            return ExitCodeConstants.Success;
        }

        // Order: in-scope first, then out-of-scope
        var orderedConnections = connections
            .OrderByDescending(c => c.IsInScope)
            .ToList();

        // Gather info for each AppHost
        var appHostInfos = await GatherAppHostInfosAsync(orderedConnections, cancellationToken).ConfigureAwait(false);

        if (format == OutputFormat.Json)
        {
            var json = JsonSerializer.Serialize(appHostInfos, PsCommandJsonContext.RelaxedEscaping.ListAppHostDisplayInfo);
            // Structured output always goes to stdout.
            _interactionService.DisplayRawText(json, ConsoleOutput.Standard);
        }
        else
        {
            DisplayTable(appHostInfos);
        }

        return ExitCodeConstants.Success;
    }

    private async Task<List<AppHostDisplayInfo>> GatherAppHostInfosAsync(List<IAppHostAuxiliaryBackchannel> connections, CancellationToken cancellationToken)
    {
        var appHostInfos = new List<AppHostDisplayInfo>();

        foreach (var connection in connections)
        {
            var info = connection.AppHostInfo;
            if (info is null)
            {
                continue;
            }

            string? dashboardUrl = null;

            try
            {
                var dashboardUrls = await connection.GetDashboardUrlsAsync(cancellationToken).ConfigureAwait(false);
                dashboardUrl = dashboardUrls?.BaseUrlWithLoginToken;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get dashboard URL for {AppHostPath}", info.AppHostPath);
            }

            appHostInfos.Add(new AppHostDisplayInfo(
                info.AppHostPath ?? PsCommandStrings.UnknownPath,
                info.ProcessId,
                info.CliProcessId,
                dashboardUrl));
        }

        return appHostInfos;
    }

    private void DisplayTable(List<AppHostDisplayInfo> appHosts)
    {
        if (appHosts.Count == 0)
        {
            return;
        }

        const string NullCliPidDisplay = "-";

        // Shorten paths appropriately
        string ShortenPath(string path)
        {
            var fileName = Path.GetFileName(path);
            
            if (string.IsNullOrEmpty(fileName))
            {
                return path;
            }

            // For .csproj files, just show the filename (folder often has same name)
            if (fileName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            // For single-file AppHosts (.cs), show parent/filename
            var directory = Path.GetDirectoryName(path);
            var parentFolder = !string.IsNullOrEmpty(directory) 
                ? Path.GetFileName(directory) 
                : null;

            return !string.IsNullOrEmpty(parentFolder)
                ? $"{parentFolder}/{fileName}"
                : fileName;
        }

        // Format dashboard URL - just return the URL as-is since modern terminals auto-detect links
        string FormatDashboardLink(string? url)
        {
            return string.IsNullOrEmpty(url) ? "-" : url;
        }

        var shortPaths = appHosts.Select(a => ShortenPath(a.AppHostPath)).ToList();

        // Calculate column widths based on data
        var pathWidth = Math.Max(PsCommandStrings.HeaderPath.Length, shortPaths.Max(p => p.Length));
        var pidWidth = Math.Max(PsCommandStrings.HeaderPid.Length, appHosts.Max(a => a.AppHostPid.ToString(CultureInfo.InvariantCulture).Length));
        var cliPidWidth = Math.Max(PsCommandStrings.HeaderCliPid.Length, appHosts.Max(a => a.CliPid?.ToString(CultureInfo.InvariantCulture).Length ?? NullCliPidDisplay.Length));

        // Header
        var header = $"{PsCommandStrings.HeaderPath.PadRight(pathWidth)}  {PsCommandStrings.HeaderPid.PadRight(pidWidth)}  {PsCommandStrings.HeaderCliPid.PadRight(cliPidWidth)}  {PsCommandStrings.HeaderDashboard}";
        _interactionService.DisplayPlainText(header);

        // Rows
        for (var i = 0; i < appHosts.Count; i++)
        {
            var appHost = appHosts[i];
            var shortPath = shortPaths[i];
            var cliPidDisplay = appHost.CliPid?.ToString(CultureInfo.InvariantCulture) ?? NullCliPidDisplay;
            var dashboardDisplay = FormatDashboardLink(appHost.DashboardUrl);
            var row = $"{shortPath.PadRight(pathWidth)}  {appHost.AppHostPid.ToString(CultureInfo.InvariantCulture).PadRight(pidWidth)}  {cliPidDisplay.PadRight(cliPidWidth)}  {dashboardDisplay}";
            _interactionService.DisplayPlainText(row);
        }
    }
}
