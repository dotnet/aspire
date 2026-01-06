// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace TerminalMcp;

/// <summary>
/// Manages multiple terminal sessions with thread-safe operations.
/// </summary>
public sealed class TerminalSessionManager : IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, TerminalSession> _sessions = new();
    private bool _disposed;

    /// <summary>
    /// Gets the number of active sessions.
    /// </summary>
    public int SessionCount => _sessions.Count;

    /// <summary>
    /// Starts a new terminal session with an auto-generated ID.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">Command arguments.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="environment">Additional environment variables.</param>
    /// <param name="width">Terminal width in columns.</param>
    /// <param name="height">Terminal height in rows.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created terminal session.</returns>
    public async Task<TerminalSession> StartSessionAsync(
        string command,
        string[] arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environment = null,
        int width = 80,
        int height = 24,
        CancellationToken ct = default)
    {
        var id = GenerateSessionId();
        return await StartSessionAsync(id, command, arguments, workingDirectory, environment, width, height, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Starts a new terminal session with a specified ID.
    /// </summary>
    /// <param name="id">The session ID. Must be unique.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="arguments">Command arguments.</param>
    /// <param name="workingDirectory">Working directory for the process.</param>
    /// <param name="environment">Additional environment variables.</param>
    /// <param name="width">Terminal width in columns.</param>
    /// <param name="height">Terminal height in rows.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created terminal session.</returns>
    /// <exception cref="InvalidOperationException">A session with the given ID already exists.</exception>
    public async Task<TerminalSession> StartSessionAsync(
        string id,
        string command,
        string[] arguments,
        string? workingDirectory = null,
        Dictionary<string, string>? environment = null,
        int width = 80,
        int height = 24,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var session = await TerminalSession.StartAsync(
            id,
            command,
            arguments,
            workingDirectory,
            environment,
            width,
            height,
            ct).ConfigureAwait(false);

        if (!_sessions.TryAdd(id, session))
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"A session with ID '{id}' already exists.");
        }

        return session;
    }

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>The session if found, null otherwise.</returns>
    public TerminalSession? GetSession(string id)
    {
        _sessions.TryGetValue(id, out var session);
        return session;
    }

    /// <summary>
    /// Gets all active sessions.
    /// </summary>
    /// <returns>A snapshot of all active sessions.</returns>
    public IReadOnlyList<TerminalSession> GetAllSessions()
    {
        return [.. _sessions.Values];
    }

    /// <summary>
    /// Lists all sessions with their basic information.
    /// </summary>
    /// <returns>Session information for all active sessions.</returns>
    public IReadOnlyList<SessionInfo> ListSessions()
    {
        return _sessions.Values.Select(s => new SessionInfo
        {
            Id = s.Id,
            Command = s.Command,
            Arguments = s.Arguments,
            WorkingDirectory = s.WorkingDirectory,
            Width = s.Width,
            Height = s.Height,
            StartedAt = s.StartedAt,
            HasExited = s.HasExited,
            ExitCode = s.HasExited ? s.ExitCode : null,
            ProcessId = s.ProcessId
        }).ToList();
    }

    /// <summary>
    /// Stops a session's process but keeps the session for inspection.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <param name="signal">Signal to send when killing (Unix only). Default is SIGTERM (15).</param>
    /// <returns>True if the session was found, false if not found.</returns>
    public bool StopSession(string id, int signal = 15)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            return false;
        }

        if (!session.HasExited)
        {
            session.Kill(signal);
        }
        return true;
    }

    /// <summary>
    /// Removes a session completely, disposing all resources.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>True if the session was found and removed, false if not found.</returns>
    public async Task<bool> RemoveSessionAsync(string id)
    {
        if (!_sessions.TryRemove(id, out var session))
        {
            return false;
        }

        await session.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Stops and removes a session.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <param name="signal">Signal to send when killing (Unix only). Default is SIGTERM (15).</param>
    /// <returns>True if the session was found and stopped, false if not found.</returns>
    public async Task<bool> StopAndRemoveSessionAsync(string id, int signal = 15)
    {
        if (!_sessions.TryRemove(id, out var session))
        {
            return false;
        }

        session.Kill(signal);
        await session.DisposeAsync().ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Stops all sessions.
    /// </summary>
    public async Task StopAllSessionsAsync()
    {
        var sessions = _sessions.Values.ToList();
        _sessions.Clear();

        foreach (var session in sessions)
        {
            session.Kill();
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Removes all sessions where the process has exited.
    /// </summary>
    /// <returns>The IDs of sessions that were cleaned up.</returns>
    public async Task<IReadOnlyList<string>> CleanupExitedSessionsAsync()
    {
        var exitedSessions = _sessions
            .Where(kvp => kvp.Value.HasExited)
            .Select(kvp => kvp.Key)
            .ToList();

        var cleanedUp = new List<string>();
        foreach (var id in exitedSessions)
        {
            if (_sessions.TryRemove(id, out var session))
            {
                await session.DisposeAsync().ConfigureAwait(false);
                cleanedUp.Add(id);
            }
        }

        return cleanedUp;
    }

    private static string GenerateSessionId()
    {
        // Generate a short, human-readable ID
        return $"term-{Guid.NewGuid():N}"[..12];
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAllSessionsAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Information about a terminal session.
/// </summary>
public sealed class SessionInfo
{
    /// <summary>
    /// The unique session ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The command being executed.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// The command arguments.
    /// </summary>
    public required IReadOnlyList<string> Arguments { get; init; }

    /// <summary>
    /// The working directory.
    /// </summary>
    public required string? WorkingDirectory { get; init; }

    /// <summary>
    /// Terminal width in columns.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Terminal height in rows.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// When the session was started.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Whether the process has exited.
    /// </summary>
    public required bool HasExited { get; init; }

    /// <summary>
    /// The exit code if the process has exited.
    /// </summary>
    public required int? ExitCode { get; init; }

    /// <summary>
    /// The process ID of the child process.
    /// </summary>
    public required int ProcessId { get; init; }
}
