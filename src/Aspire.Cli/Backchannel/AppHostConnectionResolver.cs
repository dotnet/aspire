// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Cli.Interaction;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Result of resolving an AppHost connection.
/// </summary>
internal sealed class AppHostConnectionResult
{
    public IAppHostAuxiliaryBackchannel? Connection { get; init; }

    [MemberNotNullWhen(true, nameof(Connection))]
    public bool Success => Connection is not null;

    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Discovers and resolves connections to running AppHosts when the socket path is not known.
/// Scans for running AppHosts and prompts the user to select one if multiple are found.
/// Used by CLI commands (stop, resources, logs, telemetry) that need to find a running AppHost.
/// For managing a specific instance when the socket path is known, use <see cref="Projects.RunningInstanceManager"/> instead.
/// </summary>
internal sealed class AppHostConnectionResolver(
    IAuxiliaryBackchannelMonitor backchannelMonitor,
    IInteractionService interactionService,
    CliExecutionContext executionContext,
    ILogger logger)
{
    /// <summary>
    /// Resolves an AppHost connection using socket-first discovery.
    /// </summary>
    /// <param name="projectFile">Optional project file. If specified, uses fast path to find matching socket.</param>
    /// <param name="scanningMessage">Message to display while scanning for AppHosts.</param>
    /// <param name="selectPrompt">Prompt to display when multiple AppHosts are found.</param>
    /// <param name="noInScopeMessage">Message to display when no in-scope AppHosts are found but others exist.</param>
    /// <param name="notFoundMessage">Message to display when no AppHosts are found.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resolved connection, or null with an error message.</returns>
    public async Task<AppHostConnectionResult> ResolveConnectionAsync(
        FileInfo? projectFile,
        string scanningMessage,
        string selectPrompt,
        string noInScopeMessage,
        string notFoundMessage,
        CancellationToken cancellationToken)
    {
        // Fast path: If --project was specified, check directly for its socket
        if (projectFile is not null)
        {
            var targetPath = projectFile.FullName;
            var matchingSockets = AppHostHelper.FindMatchingSockets(
                targetPath,
                executionContext.HomeDirectory.FullName);

            // Try each matching socket until we get a connection
            foreach (var socketPath in matchingSockets)
            {
                try
                {
                    var connection = await AppHostAuxiliaryBackchannel.ConnectAsync(
                        socketPath, logger, cancellationToken).ConfigureAwait(false);
                    if (connection is not null)
                    {
                        return new AppHostConnectionResult { Connection = connection };
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to connect to socket at {SocketPath}", socketPath);
                }
            }

            return new AppHostConnectionResult { ErrorMessage = notFoundMessage };
        }

        // Socket-first approach: Scan for running AppHosts via their sockets
        // This is fast because it only looks at ~/.aspire/backchannels/ directory
        // rather than recursively searching the entire directory tree for project files
        var connections = await interactionService.ShowStatusAsync(
            scanningMessage,
            async () =>
            {
                await backchannelMonitor.ScanAsync(cancellationToken).ConfigureAwait(false);
                return backchannelMonitor.Connections.ToList();
            });

        if (connections.Count == 0)
        {
            return new AppHostConnectionResult { ErrorMessage = notFoundMessage };
        }

        // Filter to in-scope AppHosts (within working directory)
        var workingDirectory = executionContext.WorkingDirectory.FullName;
        var inScopeConnections = connections.Where(c => c.IsInScope).ToList();
        var outOfScopeConnections = connections.Where(c => !c.IsInScope).ToList();

        IAppHostAuxiliaryBackchannel? selectedConnection = null;

        if (inScopeConnections.Count == 1)
        {
            // Only one in-scope AppHost, use it
            selectedConnection = inScopeConnections[0];
        }
        else if (inScopeConnections.Count > 1)
        {
            // Multiple in-scope AppHosts running, prompt for selection
            // Order by most recently started first
            var choices = inScopeConnections
                .OrderByDescending(c => c.AppHostInfo?.StartedAt ?? DateTimeOffset.MinValue)
                .Select(c =>
                {
                    var appHostPath = c.AppHostInfo?.AppHostPath ?? "Unknown";
                    var relativePath = Path.GetRelativePath(workingDirectory, appHostPath);
                    return (Display: relativePath, Connection: c);
                })
                .ToList();

            var selectedDisplay = await interactionService.PromptForSelectionAsync(
                selectPrompt,
                choices.Select(c => c.Display).ToArray(),
                c => c,
                cancellationToken);

            selectedConnection = choices.FirstOrDefault(c => c.Display == selectedDisplay).Connection;
        }
        else if (outOfScopeConnections.Count > 0)
        {
            // No in-scope AppHosts, but there are out-of-scope ones - let user pick
            interactionService.DisplayMessage("information", noInScopeMessage);

            // Order by most recently started first
            var choices = outOfScopeConnections
                .OrderByDescending(c => c.AppHostInfo?.StartedAt ?? DateTimeOffset.MinValue)
                .Select(c =>
                {
                    var path = c.AppHostInfo?.AppHostPath ?? "Unknown";
                    return (Display: path, Connection: c);
                })
                .ToList();

            var selectedDisplay = await interactionService.PromptForSelectionAsync(
                selectPrompt,
                choices.Select(c => c.Display).ToArray(),
                c => c,
                cancellationToken);

            selectedConnection = choices.FirstOrDefault(c => c.Display == selectedDisplay).Connection;
        }

        if (selectedConnection is null)
        {
            return new AppHostConnectionResult { ErrorMessage = notFoundMessage };
        }

        return new AppHostConnectionResult { Connection = selectedConnection };
    }
}
