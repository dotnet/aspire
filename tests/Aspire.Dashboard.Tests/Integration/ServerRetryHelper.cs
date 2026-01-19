// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Tests.Integration;

// Copied from https://github.com/dotnet/aspnetcore/blob/1b2e5286b089fa1cab90ba8692c2df7ca6f9c077/src/Servers/Kestrel/shared/test/ServerRetryHelper.cs
public static class ServerRetryHelper
{
    private const int RetryCount = 20;

    // Named mutex to prevent race conditions when multiple parallel tests (even across processes)
    // try to find and bind ports. Without this, GetPossibleAvailablePort can return the same port to
    // multiple tests before any of them bind.
    private const string PortAllocationMutexName = "Global\\AspireDashboardTestPortAllocation";

    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static Task BindPortWithRetry(Func<int, Task> retryFunc, ILogger logger)
    {
        return BindPortsWithRetry(ports => retryFunc(ports.Single()), logger, portCount: 1);
    }

    /// <summary>
    /// Retry a func. Useful when a test needs an explicit port and you want to avoid port conflicts.
    /// </summary>
    public static async Task BindPortsWithRetry(Func<List<int>, Task> retryFunc, ILogger logger, int portCount)
    {
        var retryCount = 0;

        // Add a random number to starting port to reduce chance of conflicts because of multiple tests using this retry.
        var nextPortAttempt = 30000 + Random.Shared.Next(10000);

        // Use a named mutex to prevent multiple parallel tests (even across processes) from
        // selecting the same port between the time we find it and the time the test binds to it.
        using var mutex = new Mutex(initiallyOwned: false, PortAllocationMutexName);

        while (true)
        {
            // Find a port that's available for TCP and UDP. Start with the given port search upwards from there.
            var ports = new List<int>(portCount);

            if (!mutex.WaitOne(TestConstants.DefaultTimeoutTimeSpan))
            {
                throw new TimeoutException($"Timed out waiting for port allocation mutex after {TestConstants.DefaultTimeoutTimeSpan}.");
            }

            try
            {
                while (ports.Count < portCount)
                {
                    if (nextPortAttempt >= ushort.MaxValue)
                    {
                        throw new InvalidOperationException($"Exhausted all available ports searching for {portCount} free ports.");
                    }

                    var port = GetPossibleAvailablePort(nextPortAttempt, logger);

                    // Verify port is actually available by trying to bind to it.
                    // This catches cases where IPGlobalProperties doesn't report the port as in use,
                    // but it's actually unavailable (e.g., TIME_WAIT state on some platforms).
                    if (TryBindPort(port, logger))
                    {
                        ports.Add(port);
                    }
                    else
                    {
                        logger.LogInformation("Port {port} found to be unavailable during bind check. Continuing search.", port);
                    }

                    // Use a minimum gap of 10 between port allocations to reduce the risk of port collisions.
                    // Allocating consecutive ports (gap of 0) can lead to conflicts if the OS or other processes
                    // allocate ports in the same range. The random gap further reduces the chance of collision.
                    nextPortAttempt = port + Random.Shared.Next(10, 100);
                }

                if (ports.Count != ports.Distinct().Count())
                {
                    throw new InvalidOperationException($"Generated ports list contains duplicate numbers: {string.Join(", ", ports)}");
                }

                // Call retryFunc inside the mutex so the ports are bound before we release.
                // This prevents another test from grabbing the same ports.
                await retryFunc(ports);

                // Success - exit the retry loop
                return;
            }
            catch (Exception ex) when (ex is not TimeoutException and not InvalidOperationException)
            {
                retryCount++;

                if (retryCount >= RetryCount)
                {
                    throw;
                }
                else
                {
                    logger.LogError(ex, "Error running test {retryCount}. Retrying.", retryCount);
                }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }

    private static int GetPossibleAvailablePort(int startingPort, ILogger logger)
    {
        logger.LogInformation("Searching for possibly free port starting at {startingPort}.", startingPort);

        var unavailableEndpoints = new List<IPEndPoint>();

        var properties = IPGlobalProperties.GetIPGlobalProperties();

        // Ignore active connections
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveTcpConnections().Select(c => c.LocalEndPoint));

        // Ignore active tcp listners
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveTcpListeners());

        // Ignore active UDP listeners
        AddEndpoints(startingPort, unavailableEndpoints, properties.GetActiveUdpListeners());

        logger.LogInformation("Found {count} unavailable endpoints.", unavailableEndpoints.Count);

        for (var i = startingPort; i < ushort.MaxValue; i++)
        {
            var match = unavailableEndpoints.FirstOrDefault(ep => ep.Port == i);
            if (match is null)
            {
                logger.LogInformation("Port {i} free.", i);
                return i;
            }
            else
            {
                logger.LogInformation("Port {i} in use. End point: {match}", i, match);
            }
        }

        throw new InvalidOperationException($"Couldn't find a free port after {startingPort}.");

        static void AddEndpoints(int startingPort, List<IPEndPoint> endpoints, IEnumerable<IPEndPoint> activeEndpoints)
        {
            foreach (IPEndPoint endpoint in activeEndpoints)
            {
                if (endpoint.Port >= startingPort)
                {
                    endpoints.Add(endpoint);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to bind to the specified port to verify it's truly available.
    /// </summary>
    private static bool TryBindPort(int port, ILogger logger)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            // Successfully bound, port is available
            return true;
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            logger.LogInformation("Port {port} bind check failed: address already in use.", port);
            return false;
        }
        catch (SocketException ex)
        {
            logger.LogWarning(ex, "Port {port} bind check failed with unexpected error.", port);
            return false;
        }
    }
}
