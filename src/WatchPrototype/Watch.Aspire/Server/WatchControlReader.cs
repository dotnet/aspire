// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class WatchControlReader : IAsyncDisposable
{
    private readonly CompilationHandler _compilationHandler;
    private readonly string _pipeName;
    private readonly NamedPipeClientStream _pipe;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _disposalCancellationSource = new();
    private readonly Task _listener;

    public WatchControlReader(string pipeName, CompilationHandler compilationHandler, ILogger logger)
    {
        _pipe = new NamedPipeClientStream(
            serverName: ".",
            pipeName,
            PipeDirection.In,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

        _pipeName = pipeName;
        _compilationHandler = compilationHandler;
        _logger = logger;
        _listener = ListenAsync(_disposalCancellationSource.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogDebug("Disposing control pipe.");

        _disposalCancellationSource.Cancel();
        await _listener;

        try
        {
            await _pipe.DisposeAsync();
        }
        catch (IOException)
        {
            // Pipe may already be broken if the server disconnected
        }
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Connecting to control pipe '{PipeName}'.", _pipeName);
            await _pipe.ConnectAsync(cancellationToken);

            using var reader = new StreamReader(_pipe);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    return;
                }

                var command = JsonSerializer.Deserialize<WatchControlCommand>(line);
                if (command is null)
                {
                    break;
                }

                if (command.Type == WatchControlCommand.Types.Rebuild)
                {
                    _logger.LogDebug("Received request to restart projects");
                    await RestartProjectsAsync(command.Projects.Select(ProjectRepresentation.FromProjectOrEntryPointFilePath), cancellationToken);
                }
                else
                {
                    _logger.LogError("Unknown control command: '{Type}'", command.Type);
                }
            }
        }
        catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or IOException)
        {
            // expected when disposing or if the server disconnects
        }
        catch (Exception e)
        {
            _logger.LogDebug("Control pipe listener failed: {Message}", e.Message);
        }
    }

    private async ValueTask RestartProjectsAsync(IEnumerable<ProjectRepresentation> projects, CancellationToken cancellationToken)
    {
        var projectsToRestart = await _compilationHandler.TerminatePeripheralProcessesAsync(projects.Select(p => p.ProjectGraphPath), cancellationToken);

        foreach (var project in projects)
        {
            if (!projectsToRestart.Any(p => p.Options.Representation == project))
            {
                _compilationHandler.Logger.LogDebug("Restart of '{Project}' requested but the project is not running.", project);
            }
        }

        await _compilationHandler.RestartPeripheralProjectsAsync(projectsToRestart, cancellationToken);
    }
}
