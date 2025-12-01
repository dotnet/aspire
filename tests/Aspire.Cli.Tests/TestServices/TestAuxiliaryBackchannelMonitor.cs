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
