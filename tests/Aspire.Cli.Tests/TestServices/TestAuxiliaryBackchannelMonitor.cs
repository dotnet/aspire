// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestAuxiliaryBackchannelMonitor : IAuxiliaryBackchannelMonitor
{
    private readonly Dictionary<string, AppHostConnection> _connections = new();
    private string? _selectedAppHostPath;

    public IReadOnlyDictionary<string, AppHostConnection> Connections => _connections;

    public string? SelectedAppHostPath
    {
        get => _selectedAppHostPath;
        set
        {
            if (_selectedAppHostPath != value)
            {
                _selectedAppHostPath = value;
                SelectedAppHostChanged?.Invoke();
            }
        }
    }

    public event Action? SelectedAppHostChanged;

    public AppHostConnection? SelectedConnection
    {
        get
        {
            var connections = _connections.Values.ToList();

            if (connections.Count == 0)
            {
                return null;
            }

            // Check if a specific AppHost was selected
            if (!string.IsNullOrEmpty(SelectedAppHostPath))
            {
                var selectedConnection = connections.FirstOrDefault(c =>
                    c.AppHostInfo?.AppHostPath != null &&
                    string.Equals(Path.GetFullPath(c.AppHostInfo.AppHostPath), Path.GetFullPath(SelectedAppHostPath), StringComparison.OrdinalIgnoreCase));

                if (selectedConnection != null)
                {
                    return selectedConnection;
                }

                // Clear the selection since the AppHost is no longer available
                SelectedAppHostPath = null;
            }

            // Look for in-scope connections
            var inScopeConnections = connections.Where(c => c.IsInScope).ToList();

            if (inScopeConnections.Count == 1)
            {
                return inScopeConnections[0];
            }

            // Fall back to the first available connection
            return connections.FirstOrDefault();
        }
    }

    public IReadOnlyList<AppHostConnection> GetConnectionsForWorkingDirectory(DirectoryInfo workingDirectory)
    {
        return _connections.Values
            .Where(c => IsAppHostInScopeOfDirectory(c.AppHostInfo?.AppHostPath, workingDirectory.FullName))
            .ToList();
    }

    private static bool IsAppHostInScopeOfDirectory(string? appHostPath, string workingDirectory)
    {
        if (string.IsNullOrEmpty(appHostPath))
        {
            return false;
        }

        // Normalize the paths for comparison
        var normalizedWorkingDirectory = Path.GetFullPath(workingDirectory);
        var normalizedAppHostPath = Path.GetFullPath(appHostPath);

        // Check if the AppHost path is within the working directory
        var relativePath = Path.GetRelativePath(normalizedWorkingDirectory, normalizedAppHostPath);
        return !relativePath.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relativePath);
    }

    public void AddConnection(string hash, AppHostConnection connection)
    {
        _connections[hash] = connection;
    }

    public void RemoveConnection(string hash)
    {
        _connections.Remove(hash);
    }

    public void ClearConnections()
    {
        _connections.Clear();
    }
}
