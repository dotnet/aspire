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

        var jsonOption = new Option<bool>("--json");
        jsonOption.Description = PsCommandStrings.JsonOptionDescription;
        Options.Add(jsonOption);
    }

    protected override async Task<int> ExecuteAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var jsonOutput = parseResult.GetValue<bool>("--json");

        // Scan for running AppHosts (same as ListAppHostsTool)
        var connections = await _interactionService.ShowStatusAsync(
            PsCommandStrings.ScanningForRunningAppHosts,
            async () =>
            {
                await _backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);
                return _backchannelMonitor.Connections.Values.ToList();
            });

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
                info.AppHostPath ?? "Unknown",
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

        // Calculate column widths
        var pathWidth = Math.Max("PATH".Length, appHosts.Max(a => a.AppHostPath.Length));
        var pidWidth = Math.Max("PID".Length, appHosts.Max(a => a.AppHostPid.ToString(CultureInfo.InvariantCulture).Length));
        var cliPidWidth = Math.Max("CLI_PID".Length, appHosts.Max(a => a.CliPid?.ToString(CultureInfo.InvariantCulture).Length ?? 1));

        // Header
        var header = $"{"PATH".PadRight(pathWidth)}  {"PID".PadRight(pidWidth)}  {"CLI_PID".PadRight(cliPidWidth)}  DASHBOARD";
        _interactionService.DisplayPlainText(header);

        // Rows
        foreach (var appHost in appHosts)
        {
            var cliPidDisplay = appHost.CliPid?.ToString(CultureInfo.InvariantCulture) ?? "-";
            var row = $"{appHost.AppHostPath.PadRight(pathWidth)}  {appHost.AppHostPid.ToString(CultureInfo.InvariantCulture).PadRight(pidWidth)}  {cliPidDisplay.PadRight(cliPidWidth)}  {appHost.DashboardUrl ?? ""}";
            _interactionService.DisplayPlainText(row);
        }
    }
}
