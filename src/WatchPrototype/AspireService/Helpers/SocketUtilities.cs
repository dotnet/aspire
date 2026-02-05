// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Aspire.Tools.Service;

internal class SocketUtilities
{
    /// <summary>
    /// Unsafe ports as defined by chrome (http://superuser.com/questions/188058/which-ports-are-considered-unsafe-on-chrome)
    /// </summary>
    private static readonly int[] s_unsafePorts = new int[] {
                2049, // nfs
                3659, // apple-sasl / PasswordServer
                4045, // lockd
                6000, // X11
                6665, // Alternate IRC [Apple addition]
                6666, // Alternate IRC [Apple addition]
                6667, // Standard IRC [Apple addition]
                6668, // Alternate IRC [Apple addition]
                6669, // Alternate IRC [Apple addition]
    };

    /// <summary>
    /// Get the next available dynamic port 
    /// </summary>
    public static int GetNextAvailablePort()
    {
        var ports = GetNextAvailablePorts(1);
        return ports == null ? 0 : ports[0];
    }

    /// <summary>
    /// Get a list of available dynamic ports. Max that can be retrieved is 10
    /// </summary>
    public static int[]? GetNextAvailablePorts(int countOfPorts)
    {
        // Creates the Socket to send data over a TCP connection.
        var ports = GetNextAvailablePorts(countOfPorts, AddressFamily.InterNetwork);
        ports ??= GetNextAvailablePorts(countOfPorts, AddressFamily.InterNetworkV6);
        return ports;
    }

    /// <summary>
    /// Get a list of available dynamic ports for the addressFamily.
    /// </summary>
    private static int[]? GetNextAvailablePorts(int countOfPorts, AddressFamily addressFamily)
    {
        // Creates the Socket to send data over a TCP connection.
        var sockets = new List<Socket>();
        try
        {
            var ports = new int[countOfPorts];
            for (int i = 0; i < countOfPorts; i++)
            {
                Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                sockets.Add(socket);
                IPEndPoint endPoint = new IPEndPoint(addressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any, 0);
                socket.Bind(endPoint);
                var endPointUsed = (IPEndPoint?)socket.LocalEndPoint;
                if (endPointUsed is not null && !s_unsafePorts.Contains(endPointUsed.Port))
                {
                    ports[i] = endPointUsed.Port;
                }
                else
                {   // Need to try this one again
                    --i;
                }
            }

            return ports;
        }
        catch (SocketException)
        {
        }
        finally
        {
            foreach (var socket in sockets)
            {
                socket.Dispose();
            }
            sockets.Clear();
        }

        return null;
    }
}
