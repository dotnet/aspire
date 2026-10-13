// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aspire.Shared;

/// <summary>
/// Describes the availability of a single container runtime (e.g., Docker, Podman, or WSLc).
/// </summary>
internal sealed class ContainerRuntimeInfo
{
    /// <summary>
    /// The executable name (e.g., "docker", "podman").
    /// </summary>
    public required string Executable { get; init; }

    /// <summary>
    /// Display name (e.g., "Docker", "Podman").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the runtime CLI was found on PATH.
    /// </summary>
    public bool IsInstalled { get; init; }

    /// <summary>
    /// Whether the runtime daemon/service is responding.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Discovery priority when availability is equal. Lower values are preferred.
    /// </summary>
    public int Priority { get; init; } = int.MaxValue;

    /// <summary>
    /// Error message if detection failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The client (CLI) version, if detected.
    /// </summary>
    public Version? ClientVersion { get; init; }

    /// <summary>
    /// The server (daemon/engine) version, if detected.
    /// </summary>
    public Version? ServerVersion { get; init; }

    /// <summary>
    /// Whether this is Docker Desktop (vs Docker Engine).
    /// </summary>
    public bool IsDockerDesktop { get; init; }

    /// <summary>
    /// The server OS (e.g., "linux", "windows"). Relevant for Docker's Windows container mode.
    /// </summary>
    public string? ServerOs { get; init; }

    /// <summary>
    /// Whether the runtime is fully operational.
    /// </summary>
    public bool IsHealthy => IsInstalled && IsRunning;
}

/// <summary>
/// Shares availability probing and Docker-compatible version parsing between container runtimes.
/// </summary>
internal class ContainerRuntimeProbe(string executable, string name, int priority)
{
    private static readonly TimeSpan s_processTimeout = TimeSpan.FromSeconds(10);

    public string Executable => executable;
    public string Name => name;
    public int Priority => priority;

    /// <summary>
    /// Returns an actionable host-compatibility error, or null when the host is supported.
    /// </summary>
    public virtual string? GetUnsupportedHostError(bool isWindows) => null;

    /// <summary>
    /// Checks CLI installation and runtime health, then gathers available version metadata.
    /// </summary>
    public async Task<ContainerRuntimeInfo> CheckAsync(ILogger? logger, CancellationToken cancellationToken)
    {
        if (GetUnsupportedHostError(OperatingSystem.IsWindows()) is string hostError)
        {
            logger?.LogDebug("{Name}: {Error}", name, hostError);
            return new ContainerRuntimeInfo
            {
                Executable = executable,
                Name = name,
                Priority = priority,
                Error = hostError
            };
        }

        try
        {
            logger?.LogDebug("Probing container runtime '{Name}' ({Executable})...", name, executable);
            // Check CLI availability and runtime health with `<runtime> container ls -n 1`.
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "container ls -n 1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new ContainerRuntimeInfo
                {
                    Executable = executable,
                    Name = name,
                    IsInstalled = false,
                    IsRunning = false,
                    Priority = priority,
                    Error = $"{name} CLI not found on PATH."
                };
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try { process.Kill(); } catch { /* best effort */ }
                return new ContainerRuntimeInfo
                {
                    Executable = executable,
                    Name = name,
                    IsInstalled = true,
                    IsRunning = false,
                    Priority = priority,
                    Error = $"{name} CLI timed out while checking status."
                };
            }

            if (process.ExitCode == 0)
            {
                // Runtime is running — gather version metadata
                logger?.LogDebug("{Name} is running, gathering version info...", name);
                var versionInfo = await ReadVersionInfoAsync(isRunning: true, logger, cancellationToken).ConfigureAwait(false);
                logger?.LogDebug("{Name}: client={ClientVersion}, server={ServerVersion}, desktop={IsDesktop}", name, versionInfo.ClientVersion, versionInfo.ServerVersion, versionInfo.IsDockerDesktop);

                return new ContainerRuntimeInfo
                {
                    Executable = executable,
                    Name = name,
                    IsInstalled = true,
                    IsRunning = true,
                    Priority = priority,
                    ClientVersion = versionInfo.ClientVersion,
                    ServerVersion = versionInfo.ServerVersion,
                    IsDockerDesktop = versionInfo.IsDockerDesktop,
                    ServerOs = versionInfo.ServerOs
                };
            }

            // Non-zero exit code: CLI exists (we started it) but daemon may not be running.
            var isInstalled = await IsCliInstalledAsync(executable, cancellationToken).ConfigureAwait(false);
            logger?.LogDebug("{Name}: exit code {ExitCode}, installed={IsInstalled}", name, process.ExitCode, isInstalled);

            var partialVersionInfo = isInstalled
                ? await ReadVersionInfoAsync(isRunning: false, logger, cancellationToken).ConfigureAwait(false)
                : default;

            var error = isInstalled
                ? $"{name} is installed but the daemon is not running."
                : $"{name} CLI not found on PATH.";
            logger?.LogDebug("{Name}: {Error}", name, error);

            return new ContainerRuntimeInfo
            {
                Executable = executable,
                Name = name,
                IsInstalled = isInstalled,
                IsRunning = false,
                Priority = priority,
                ClientVersion = partialVersionInfo.ClientVersion,
                IsDockerDesktop = partialVersionInfo.IsDockerDesktop,
                Error = error
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            logger?.LogDebug("{Name}: not found on PATH ({ExceptionMessage})", name, ex.Message);
            return new ContainerRuntimeInfo
            {
                Executable = executable,
                Name = name,
                IsInstalled = false,
                IsRunning = false,
                Priority = priority,
                Error = $"{name} CLI not found on PATH."
            };
        }
    }

    private static async Task<bool> IsCliInstalledAsync(string executable, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return false;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try { process.Kill(); } catch { /* best effort */ }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Reads the Docker/Podman version protocol, including partial output when the service is unavailable.
    /// </summary>
    protected virtual async Task<RuntimeVersionInfo> ReadVersionInfoAsync(bool isRunning, ILogger? logger, CancellationToken cancellationToken)
    {
        var output = await ReadVersionOutputAsync("version -f json", logger, cancellationToken).ConfigureAwait(false);
        return ParseVersionOutput(output);
    }

    /// <summary>
    /// Captures output from a runtime metadata command using the common process and timeout handling.
    /// </summary>
    protected async Task<string?> ReadVersionOutputAsync(string arguments, ILogger? logger, CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(s_processTimeout);

            string output;
            try
            {
                output = await process.StandardOutput.ReadToEndAsync(timeoutCts.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                try { process.Kill(); } catch { /* best effort */ }
                logger?.LogDebug("{Name} metadata command timed out.", name);
                return null;
            }

            return output;
        }
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Failed to read {Name} version metadata.", name);
            return null;
        }
    }

    /// <summary>
    /// Parses Docker-compatible version output using source-generated JSON serialization.
    /// </summary>
    /// <example>
    /// Docker:
    /// <code>
    /// {"Client":{"Version":"28.0.1","Context":"desktop-linux"},"Server":{"Version":"27.5.0","Os":"linux"}}
    /// </code>
    /// Podman:
    /// <code>
    /// {"Client":{"Version":"4.9.3"},"Server":null}
    /// </code>
    /// </example>
    internal static RuntimeVersionInfo ParseVersionOutput(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return default;
        }

        try
        {
            var json = JsonSerializer.Deserialize(output, ContainerRuntimeJsonContext.Default.ContainerRuntimeVersionJson);
            if (json is null)
            {
                return default;
            }

            Version.TryParse(json.Client?.Version, out var clientVersion);
            Version.TryParse(json.Server?.Version, out var serverVersion);
            var context = json.Client?.Context;
            var isDockerDesktop = context is not null &&
                context.Contains("desktop", StringComparison.OrdinalIgnoreCase);

            return new RuntimeVersionInfo(clientVersion, serverVersion, isDockerDesktop, json.Server?.Os);
        }
        catch (JsonException)
        {
            // Fall back to regex parsing for non-JSON output
            var match = Regex.Match(output, @"[Vv]ersion\s*:?\s*(\d+\.\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (match.Success && Version.TryParse(match.Groups[1].Value, out var version))
            {
                return new RuntimeVersionInfo(version, null, false, null);
            }

            return default;
        }
    }

    internal readonly record struct RuntimeVersionInfo(
        Version? ClientVersion,
        Version? ServerVersion,
        bool IsDockerDesktop,
        string? ServerOs);
}

internal sealed class ContainerRuntimeVersionJson
{
    [JsonPropertyName("Client")]
    public ContainerRuntimeComponentJson? Client { get; set; }

    [JsonPropertyName("Server")]
    public ContainerRuntimeComponentJson? Server { get; set; }
}

internal sealed class ContainerRuntimeComponentJson
{
    [JsonPropertyName("Version")]
    public string? Version { get; set; }

    [JsonPropertyName("Context")]
    public string? Context { get; set; }

    [JsonPropertyName("Os")]
    public string? Os { get; set; }
}

[JsonSerializable(typeof(ContainerRuntimeVersionJson))]
internal sealed partial class ContainerRuntimeJsonContext : JsonSerializerContext
{
}
