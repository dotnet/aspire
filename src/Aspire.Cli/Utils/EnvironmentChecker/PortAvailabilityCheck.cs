// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Utils.EnvironmentChecker;

/// <summary>
/// Checks if configured ports in apphost.run.json or launchSettings.json fall within
/// Windows excluded port ranges (e.g., Hyper-V dynamic reservations) or are otherwise unavailable.
/// </summary>
internal sealed class PortAvailabilityCheck(CliExecutionContext executionContext, ILogger<PortAvailabilityCheck> logger) : IEnvironmentCheck
{
    private static readonly TimeSpan s_netshTimeout = TimeSpan.FromSeconds(5);

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

            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // On Windows, check excluded port ranges (Hyper-V reservations)
            var excludedRanges = isWindows ? GetExcludedPortRanges() : [];
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
                var hasExcludedPort = blockedPorts.Any(p => IsPortExcluded(p.Port, excludedRanges));

                var details = (isWindows && hasExcludedPort)
                    ? "Some configured ports fall within a Windows excluded port range (often reserved by Hyper-V) or are otherwise unavailable. The dashboard link displayed by 'aspire run' might not work."
                    : "Some configured ports are unavailable on this machine. The dashboard link displayed by 'aspire run' might not work.";

                var fix = "Delete apphost.run.json (or launchSettings.json) and run 'aspire run' to auto-assign available ports, or update the file with ports that are currently available.";
                if (isWindows && excludedRanges.Count > 0)
                {
                    fix += "\nOn Windows, run 'netsh interface ipv4 show excludedportrange protocol=tcp' to see reserved port ranges and choose ports outside them.";
                }

                results.Add(new EnvironmentCheckResult
                {
                    Category = "environment",
                    Name = "port-availability",
                    Status = EnvironmentCheckStatus.Warning,
                    Message = $"Configured ports unavailable: {portList}",
                    Details = details,
                    Fix = fix
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

            // Check if any configured ports fall in the ephemeral port range (high risk of random conflicts)
            var ephemeralRange = GetEphemeralPortRange();
            if (ephemeralRange is not null)
            {
                var ephemeralPorts = ports
                    .Where(p => p.Port >= ephemeralRange.Value.Start && p.Port <= ephemeralRange.Value.End)
                    .ToList();

                if (ephemeralPorts.Count > 0)
                {
                    var portList = string.Join(", ", ephemeralPorts.Select(p => $"{p.Port} ({p.Source})"));
                    results.Add(new EnvironmentCheckResult
                    {
                        Category = "environment",
                        Name = "ephemeral-port-range",
                        Status = EnvironmentCheckStatus.Warning,
                        Message = $"Configured ports in ephemeral range: {portList}",
                        Details = $"These ports fall within the OS ephemeral port range ({ephemeralRange.Value.Start}-{ephemeralRange.Value.End}), which is used for outgoing connections and randomly assigned ports. This increases the chance of port conflicts.",
                        Fix = "Consider using ports outside the ephemeral range. Delete apphost.run.json (or launchSettings.json) and run 'aspire run' to auto-assign available ports, or update the file with ports below the ephemeral range (e.g., 15000-15100)."
                    });
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to check port availability");

            results.Add(new EnvironmentCheckResult
            {
                Category = "environment",
                Name = "port-availability",
                Status = EnvironmentCheckStatus.Warning,
                Message = "Unable to check port availability",
                Details = ex.Message
            });
        }

        return Task.FromResult<IReadOnlyList<EnvironmentCheckResult>>(results);
    }

    /// <summary>
    /// Reads configured ports from apphost.run.json or launchSettings.json using shared launch profile parsing.
    /// </summary>
    internal static List<(int Port, string Source)> ReadConfiguredPorts(DirectoryInfo workingDirectory)
    {
        var envVars = LaunchProfileHelper.ReadEnvironmentVariables(workingDirectory);
        if (envVars is null)
        {
            return [];
        }

        return ExtractPortsFromEnvironmentVariables(envVars);
    }

    /// <summary>
    /// Extracts port numbers from a set of environment variables (ASPNETCORE_URLS and *ENDPOINT_URL* vars).
    /// </summary>
    internal static List<(int Port, string Source)> ExtractPortsFromEnvironmentVariables(Dictionary<string, string> envVars)
    {
        var ports = new List<(int Port, string Source)>();

        // Extract ports from ASPNETCORE_URLS (mapped from applicationUrl)
        if (envVars.TryGetValue("ASPNETCORE_URLS", out var urls))
        {
            foreach (var url in urls.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Port > 0)
                {
                    ports.Add((uri.Port, "applicationUrl"));
                }
            }
        }

        // Extract ports from endpoint URL environment variables
        foreach (var (key, value) in envVars)
        {
            if (key != "ASPNETCORE_URLS" &&
                key.Contains("ENDPOINT_URL", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                ports.Add((uri.Port, key));
            }
        }

        return ports;
    }

    /// <summary>
    /// Gets the Windows excluded port ranges using netsh.
    /// </summary>
    internal static List<(int Start, int End)> GetExcludedPortRanges()
    {
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

            // Use async read with timeout to avoid hanging if netsh blocks
            var outputTask = process.StandardOutput.ReadToEndAsync();
            if (!outputTask.Wait(s_netshTimeout))
            {
                try { process.Kill(); } catch { }
                return [];
            }

            process.WaitForExit((int)s_netshTimeout.TotalMilliseconds);
            return ParseExcludedPortRanges(outputTask.Result);
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Parses the output of 'netsh interface ipv4 show excludedportrange protocol=tcp'.
    /// </summary>
    internal static List<(int Start, int End)> ParseExcludedPortRanges(string netshOutput)
    {
        var ranges = new List<(int Start, int End)>();

        foreach (var line in netshOutput.Split('\n'))
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

        return ranges;
    }

    /// <summary>
    /// Gets the OS ephemeral (dynamic) port range.
    /// On Windows: uses 'netsh int ipv4 show dynamicport tcp'.
    /// On Linux: reads /proc/sys/net/ipv4/ip_local_port_range.
    /// On macOS: uses sysctl net.inet.ip.portrange.first/last.
    /// </summary>
    internal static (int Start, int End)? GetEphemeralPortRange()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsEphemeralPortRange();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxEphemeralPortRange();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOSEphemeralPortRange();
            }
        }
        catch
        {
            // Fall through to default
        }

        // IANA default ephemeral range
        return (49152, 65535);
    }

    private static (int Start, int End)? GetWindowsEphemeralPortRange()
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = "int ipv4 show dynamicport tcp",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        if (!outputTask.Wait(s_netshTimeout))
        {
            try { process.Kill(); } catch { }
            return null;
        }

        process.WaitForExit((int)s_netshTimeout.TotalMilliseconds);
        return ParseDynamicPortRange(outputTask.Result);
    }

    /// <summary>
    /// Parses the output of 'netsh int ipv4 show dynamicport tcp'.
    /// </summary>
    internal static (int Start, int End)? ParseDynamicPortRange(string netshOutput)
    {
        int? startPort = null;
        int? numberOfPorts = null;

        foreach (var line in netshOutput.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Start Port", StringComparison.OrdinalIgnoreCase))
            {
                var colonIdx = trimmed.IndexOf(':');
                if (colonIdx >= 0 && int.TryParse(trimmed[(colonIdx + 1)..].Trim(), out var val))
                {
                    startPort = val;
                }
            }
            else if (trimmed.StartsWith("Number of Ports", StringComparison.OrdinalIgnoreCase))
            {
                var colonIdx = trimmed.IndexOf(':');
                if (colonIdx >= 0 && int.TryParse(trimmed[(colonIdx + 1)..].Trim(), out var val))
                {
                    numberOfPorts = val;
                }
            }
        }

        if (startPort.HasValue && numberOfPorts.HasValue)
        {
            return (startPort.Value, startPort.Value + numberOfPorts.Value - 1);
        }

        return null;
    }

    private static (int Start, int End)? GetLinuxEphemeralPortRange()
    {
        const string path = "/proc/sys/net/ipv4/ip_local_port_range";
        if (!File.Exists(path))
        {
            return null;
        }

        var content = File.ReadAllText(path).Trim();
        var parts = content.Split('\t', ' ');
        if (parts.Length >= 2 &&
            int.TryParse(parts[0].Trim(), out var start) &&
            int.TryParse(parts[^1].Trim(), out var end))
        {
            return (start, end);
        }

        return null;
    }

    private static (int Start, int End)? GetMacOSEphemeralPortRange()
    {
        static int? ReadSysctl(string key)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "sysctl",
                Arguments = $"-n {key}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(3000);
            return int.TryParse(output, out var val) ? val : null;
        }

        var first = ReadSysctl("net.inet.ip.portrange.first");
        var last = ReadSysctl("net.inet.ip.portrange.last");

        if (first.HasValue && last.HasValue)
        {
            return (first.Value, last.Value);
        }

        return null;
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
