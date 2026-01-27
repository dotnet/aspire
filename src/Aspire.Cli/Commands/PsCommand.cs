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
}

internal sealed class PsCommand : BaseCommand
{
    private readonly IInteractionService _interactionService;
    private readonly IAuxiliaryBackchannelMonitor _backchannelMonitor;
    private readonly ILogger<PsCommand> _logger;
    private static readonly Option<bool> s_jsonOption = new("--json")
    {
        Description = PsCommandStrings.JsonOptionDescription
    };

    public PsCommand(
        IInteractionService interactionService,
        IAuxiliaryBackchannelMonitor backchannelMonitor,
        IFeatures features,
        ICliUpdateNotifier updateNotifier,
        CliExecutionContext executionContext,
        ILogger<PsCommand> logger)
        : base("ps", PsCommandStrings.Description, features, updateNotifier, executionContext, interactionService)
    {
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(backchannelMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _interactionService = interactionService;
        _backchannelMonitor = backchannelMonitor;
        _logger = logger;

        Options.Add(s_jsonOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var jsonOutput = parseResult.GetValue(s_jsonOption);

        // Scan for running AppHosts (same as ListAppHostsTool)
        // Skip status display for JSON output to avoid contaminating stdout
        List<AppHostAuxiliaryBackchannel> connections;
        if (jsonOutput)
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
            if (jsonOutput)
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

        if (jsonOutput)
        {
            var json = JsonSerializer.Serialize(appHostInfos, PsCommandJsonContext.Default.ListAppHostDisplayInfo);
            _interactionService.DisplayPlainText(json);
        }
        else
        {
            DisplayTable(appHostInfos);
        }

        return ExitCodeConstants.Success;
    }

    private async Task<List<AppHostDisplayInfo>> GatherAppHostInfosAsync(List<AppHostAuxiliaryBackchannel> connections, CancellationToken cancellationToken)
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

        // Calculate column widths
        var pathWidth = Math.Max(PsCommandStrings.HeaderPath.Length, appHosts.Max(a => a.AppHostPath.Length));
        var pidWidth = Math.Max(PsCommandStrings.HeaderPid.Length, appHosts.Max(a => a.AppHostPid.ToString(CultureInfo.InvariantCulture).Length));
        var cliPidWidth = Math.Max(PsCommandStrings.HeaderCliPid.Length, appHosts.Max(a => a.CliPid?.ToString(CultureInfo.InvariantCulture).Length ?? NullCliPidDisplay.Length));

        // Header
        var header = $"{PsCommandStrings.HeaderPath.PadRight(pathWidth)}  {PsCommandStrings.HeaderPid.PadRight(pidWidth)}  {PsCommandStrings.HeaderCliPid.PadRight(cliPidWidth)}  {PsCommandStrings.HeaderDashboard}";
        _interactionService.DisplayPlainText(header);

        // Rows
        foreach (var appHost in appHosts)
        {
            var cliPidDisplay = appHost.CliPid?.ToString(CultureInfo.InvariantCulture) ?? NullCliPidDisplay;
            var row = $"{appHost.AppHostPath.PadRight(pathWidth)}  {appHost.AppHostPid.ToString(CultureInfo.InvariantCulture).PadRight(pidWidth)}  {cliPidDisplay.PadRight(cliPidWidth)}  {appHost.DashboardUrl ?? ""}";
            _interactionService.DisplayPlainText(row);
        }
    }
}
