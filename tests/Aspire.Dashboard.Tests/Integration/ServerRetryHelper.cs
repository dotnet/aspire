// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Tests.Integration;

// Copied from https://github.com/dotnet/aspnetcore/blob/1b2e5286b089fa1cab90ba8692c2df7ca6f9c077/src/Servers/Kestrel/shared/test/ServerRetryHelper.cs
public static class ServerRetryHelper
{
    private const int RetryCount = 20;

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

        while (true)
        {
            // Find a port that's available for TCP and UDP. Start with the given port search upwards from there.
            var ports = new List<int>(portCount);
            for (var i = 0; i < portCount; i++)
            {
                var port = GetAvailablePort(nextPortAttempt, logger);
                ports.Add(port);

                // Use a minimum gap of 10 between port allocations to reduce the risk of port collisions.
                // Allocating consecutive ports (gap of 0) can lead to conflicts if the OS or other processes
                // allocate ports in the same range. The random gap further reduces the chance of collision.
                nextPortAttempt = port + Random.Shared.Next(10, 100);
            }

            if (ports.Count != ports.Distinct().Count())
            {
                throw new InvalidOperationException($"Generated ports list contains duplicate numbers: {string.Join(", ", ports)}");
            }

            try
            {
                await retryFunc(ports);
                break;
            }
            catch (Exception ex)
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
        }
    }

    private static int GetAvailablePort(int startingPort, ILogger logger)
    {
        logger.LogInformation("Searching for free port starting at {startingPort}.", startingPort);

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
            if (match == null)
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
}
