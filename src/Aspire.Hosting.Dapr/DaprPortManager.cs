// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Net.NetworkInformation;

namespace Aspire.Hosting.Dapr;

internal sealed class DaprPortManager
{
    private IImmutableSet<int> _reservedPorts = ImmutableHashSet<int>.Empty;
    private readonly object _reservationLock = new();

    public int ReservePort(int rangeStart)
    {
        lock (this._reservationLock)
        {
            var globalProperties = IPGlobalProperties.GetIPGlobalProperties();

            var activePorts =
                globalProperties
                    .GetActiveTcpListeners()
                    .Select(endPoint => endPoint.Port)
                    .ToImmutableHashSet();

            var availablePort =
                Enumerable
                    .Range(rangeStart, Int32.MaxValue - rangeStart + 1)
                    .Where(port => !activePorts.Contains(port))
                    .Where(port => !this._reservedPorts.Contains(port))
                    .FirstOrDefault();

            if (availablePort is 0)
            {
                throw new InvalidOperationException("No available ports could be found.");
            }

            this._reservedPorts = this._reservedPorts.Add(availablePort);

            return availablePort;
        }
    }
}
