// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

internal class DevTunnelTool(ILogger<DevTunnelTool> logger) : IDevTunnelTool
{
    private async Task<T> ExecuteJsonAsync<T>(string command, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo("devtunnel", command)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        logger.LogDebug("Executing DevTunnel command: devtunnel {Command}", command);

        var process = Process.Start(startInfo);

        if (process == null)
        {
            logger.LogError("Failed to start DevTunnel process: devtunnel {Command}", command);
            // TODO: Do we need to do something here to redact values that we pass in?
            throw new DevTunnelToolException("DevTunnel process failed to start.", $"devtunnel {command}");
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            logger.LogError(stderr, "DevTunnel process failed with exit code {ExitCode}: devtunnel {Command}", process.ExitCode, command);
            throw new DevTunnelToolException("DevTunnel process with non-zero exit code.", $"devtunnel {command}", stdout, stderr);
        }
        else
        {
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(stdout) ?? throw new DevTunnelToolException("Failed to deserialize JSON output.", $"devtunnel {command}", stdout);
        }
    }

    public async Task<DevTunnelListCommandResponse> ListTunnelsAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteJsonAsync<DevTunnelListCommandResponse>("list --json", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevTunnelListPortCommandResponse> ListTunnelPortsAsync(string tunnelId, CancellationToken cancellationToken = default)
    {
        return await ExecuteJsonAsync<DevTunnelListPortCommandResponse>($"port list {tunnelId} --json", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevTunnelShowPortCommandResponse> ShowTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default)
    {
        return await ExecuteJsonAsync<DevTunnelShowPortCommandResponse>($"port show {tunnelId} --port {port} --json", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevTunnelDeletePortCommandResponse> DeleteTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default)
    {
        return await ExecuteJsonAsync<DevTunnelDeletePortCommandResponse>($"port delete {tunnelId} --port-number {port} --json", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevTunnelCreatePortCommandResponse> CreateTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default)
    {
        return await ExecuteJsonAsync<DevTunnelCreatePortCommandResponse>($"port create {tunnelId} --port-number {port} --json", cancellationToken).ConfigureAwait(false);
    }

    public async Task<DevTunnelCreateCommandResponse> CreateTunnelAsync(string tunnelId, bool allowAnonymous, CancellationToken cancellationToken = default)
    {
        var anonymousOptionSuffix = allowAnonymous ? " --allow-anonymous" : string.Empty;
        var command = $"create {tunnelId}{anonymousOptionSuffix} --json";
        return await ExecuteJsonAsync<DevTunnelCreateCommandResponse>(command, cancellationToken).ConfigureAwait(false);
    }
}
