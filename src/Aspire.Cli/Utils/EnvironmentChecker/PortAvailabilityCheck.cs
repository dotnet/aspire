// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if configured ports in apphost.run.json or launchSettings.json fall within
/// Windows excluded port ranges (e.g., Hyper-V dynamic reservations).
/// </summary>
internal sealed class PortAvailabilityCheck(CliExecutionContext executionContext, ILogger<PortAvailabilityCheck> logger) : IEnvironmentCheck
{
    public int Order => 45; // After container checks

    public Task<IReadOnlyList<EnvironmentCheckResult>> CheckAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<EnvironmentCheckResult>();

        try
        {
            var ports = ReadConfiguredPorts(executionContext.WorkingDirectory);
            if (ports.Count == 0)
            {
                return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>([]);
            }

            // On Windows, check excluded port ranges (Hyper-V reservations)
            var excludedRanges = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GetExcludedPortRanges()
                : [];
            var blockedPorts = new List<(int Port, string Source)>();

            foreach (var (port, source) in ports)
            {
                if (IsPortExcluded(port, excludedRanges))
                {
                    blockedPorts.Add((port, source));
                }
                else if (!IsPortAvailable(port))
                {
                    blockedPorts.Add((port, source));
                }
            }

            if (blockedPorts.Count > 0)
            {
                var portList = string.Join(", ", blockedPorts.Select(p => $"{p.Port} ({p.Source})"));
                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "port-availability",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"Configured ports unavailable: {portList}",
                    Details = "These ports fall within a Windows excluded port range (often reserved by Hyper-V) or are otherwise unavailable. The dashboard link displayed by 'aspire run' will not work.",
                    Fix = "Delete apphost.run.json (or launchSettings.json) and run 'aspire run' to auto-assign available ports, or update the file with ports outside excluded ranges.\nRun 'netsh interface ipv4 show excludedportrange protocol=tcp' to see reserved ranges."
                });
            }
            else
            {
                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "port-availability",
                    Status = EnvironmentCheckStatus.Pass,
                    Message = "Configured ports are available"
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to check port availability");
        }

        return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>(results);
    }

    /// <summary>
    /// Reads configured ports from apphost.run.json or launchSettings.json.
    /// </summary>
    internal static List<(int Port, string Source)> ReadConfiguredPorts(DirectoryInfo workingDirectory)
    {
        var ports = new List<(int Port, string Source)>();

        var apphostRunPath = Path.Combine(workingDirectory.FullName, "apphost.run.json");
        var launchSettingsPath = Path.Combine(workingDirectory.FullName, "Properties", "launchSettings.json");
        var configPath = File.Exists(apphostRunPath) ? apphostRunPath : launchSettingsPath;

        if (!File.Exists(configPath))
        {
            return ports;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("profiles", out var profiles))
            {
                return ports;
            }

            // Try "https" profile first, then fall back to first profile
            JsonElement? profileElement = null;
            if (profiles.TryGetProperty("https", out var httpsProfile))
            {
                profileElement = httpsProfile;
            }
            else
            {
                using var enumerator = profiles.EnumerateObject();
                if (enumerator.MoveNext())
                {
                    profileElement = enumerator.Current.Value;
                }
            }

            if (profileElement is null)
            {
                return ports;
            }

            // Extract ports from applicationUrl
            if (profileElement.Value.TryGetProperty("applicationUrl", out var appUrl) &&
                appUrl.ValueKind == JsonValueKind.String)
            {
                foreach (var url in appUrl.GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Port > 0)
                    {
                        ports.Add((uri.Port, "applicationUrl"));
                    }
                }
            }

            // Extract ports from environment variable URLs
            if (profileElement.Value.TryGetProperty("environmentVariables", out var envVars))
            {
                foreach (var prop in envVars.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String &&
                        prop.Name.Contains("ENDPOINT_URL", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = prop.Value.GetString();
                        if (value is not null && Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Port > 0)
                        {
                            ports.Add((uri.Port, prop.Name));
                        }
                    }
                }
            }
        }
        catch
        {
            // If we can't parse the file, skip this check
        }

        return ports;
    }

    /// <summary>
    /// Gets the Windows excluded port ranges using netsh.
    /// </summary>
    internal static List<(int Start, int End)> GetExcludedPortRanges()
    {
        var ranges = new List<(int Start, int End)>();

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "interface ipv4 show excludedportrange protocol=tcp",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out var start) &&
                    int.TryParse(parts[1], out var end))
                {
                    ranges.Add((start, end));
                }
            }
        }
        catch
        {
            // If netsh fails, we can't determine excluded ranges
        }

        return ranges;
    }

    private static bool IsPortExcluded(int port, List<(int Start, int End)> excludedRanges)
    {
        foreach (var (start, end) in excludedRanges)
        {
            if (port >= start && port <= end)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
