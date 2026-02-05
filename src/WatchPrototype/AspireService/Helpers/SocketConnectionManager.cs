// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Aspire.Tools.Service;

/// <summary>
/// Manages the set of active socket connections. Since it registers to be notified when a socket has gone bad,
/// it also tracks those CancellationTokenRegistration objects so they can be disposed
/// </summary>
internal class SocketConnectionManager : IDisposable
{
    // Track a single connection per Dcp ID
    private readonly object _socketConnectionsLock = new();
    private readonly Dictionary<string, WebSocketConnection> _webSocketConnections = new(StringComparer.Ordinal);

    private void CleanupSocketConnections()
    {
        lock (_socketConnectionsLock)
        {
            foreach (var connection in _webSocketConnections)
            {
                connection.Value.Tcs.SetResult();
                connection.Value.CancelTokenRegistration.Dispose();
            }

            _webSocketConnections.Clear();
        }
    }

    public void AddSocketConnection(WebSocket socket, TaskCompletionSource tcs, string dcpId, CancellationToken httpRequestAborted)
    {
        // We only support one connection per DCP Id, therefore if there is
        // already a connection, drop that one before adding this one
        lock (_socketConnectionsLock)
        {
            if (_webSocketConnections.TryGetValue(dcpId, out var existingConnection))
            {
                _webSocketConnections.Remove(dcpId);
                existingConnection.Dispose();
            }

            // Register with the cancel token so that if the socket goes bad, we
            // get notified and can remove it from our list. We need to track the registrations as well
            // so we can dispose of it later
            var newConnection = new WebSocketConnection(socket, tcs, dcpId, httpRequestAborted);
            newConnection.CancelTokenRegistration = httpRequestAborted.Register(() =>
            {
                RemoveSocketConnection(newConnection);
            });

            _webSocketConnections[dcpId] = newConnection;
        }
    }

    public void RemoveSocketConnection(WebSocketConnection connection)
    {
        lock (_socketConnectionsLock)
        {
            _webSocketConnections.Remove(connection.DcpId);
            connection.Dispose();
        }
    }

    public WebSocketConnection? GetSocketConnection(string dcpId)
    {
        lock (_socketConnectionsLock)
        {
            _webSocketConnections.TryGetValue(dcpId, out var connection);
            return connection;
        }
    }

    public void Dispose()
    {
        CleanupSocketConnections();
    }
}
