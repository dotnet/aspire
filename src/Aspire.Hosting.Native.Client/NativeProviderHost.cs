// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Native.Client;

/// <summary>Runs an integration's generated provider contract using framework-owned activation coordinates.</summary>
public static class NativeProviderHost
{
    /// <summary>Registers a generated contract and remains alive until the native peer disconnects.</summary>
    public static async Task RunAsync(NativeProviderDefinition definition, TimeProvider clock, ILogger logger,
        CancellationToken cancellationToken)
    {
        var endpoint = IPEndPoint.Parse(Environment.GetEnvironmentVariable("ASPIRE_NATIVE_RPC_ENDPOINT")
            ?? throw new InvalidOperationException("Provider activation requires a native endpoint."));
        var token = Environment.GetEnvironmentVariable("ASPIRE_NATIVE_RPC_TOKEN")
            ?? throw new InvalidOperationException("Provider activation requires authentication.");
        var authority = Environment.GetEnvironmentVariable("ASPIRE_INTEGRATION_HOST_REGISTRATION_ID")
            ?? throw new InvalidOperationException("Provider activation requires expected registration authority.");
        var connected = await NativeClient.ConnectAsync(endpoint, token, TimeSpan.FromSeconds(15), clock, cancellationToken).ConfigureAwait(false);
        await using var clientLifetime = connected.ConfigureAwait(false);
        var client = connected;
        await client.RegisterIntegrationHostAsync(authority, definition, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Native integration provider registered.");
        await client.WaitForDisconnectAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Activates an explicitly selected provider process; application code never performs registration or handle plumbing.</summary>
public sealed class NativeProviderActivation : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task _stdout;
    private readonly Task _stderr;
    private readonly TimeProvider _clock;

    private NativeProviderActivation(Process process, TimeProvider clock)
    {
        _process = process;
        _clock = clock;
        // Drain both streams concurrently. Only classifications are logged by the
        // framework; provider console output may include application secrets.
        _stdout = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
        _stderr = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
    }

    /// <summary>Launches one explicitly selected provider and awaits server-verified registration.</summary>
    public static async Task<NativeProviderActivation> StartAsync(NativeClient client, string packageName,
        string executable, IReadOnlyList<string> arguments, TimeProvider clock, ILogger logger, CancellationToken cancellationToken)
    {
        if (client.Endpoint is not IPEndPoint endpoint)
        {
            throw new NotSupportedException("This provider activation entry point currently requires loopback TCP.");
        }
        var authority = await client.ExpectIntegrationHostAsync(packageName, cancellationToken).ConfigureAwait(false);
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        start.Environment["ASPIRE_NATIVE_RPC_ENDPOINT"] = endpoint.ToString();
        start.Environment["ASPIRE_NATIVE_RPC_TOKEN"] = client.AuthenticationToken;
        start.Environment["ASPIRE_INTEGRATION_HOST_REGISTRATION_ID"] = authority;
        var activation = new NativeProviderActivation(Process.Start(start) ?? throw new InvalidOperationException("The provider process did not start."), clock);
        try
        {
            await client.WaitIntegrationHostRegistrationAsync(authority, cancellationToken).ConfigureAwait(false);
            if (activation._process.HasExited)
            {
                throw new InvalidOperationException("The provider disconnected during activation.");
            }
            logger.LogInformation("Native integration provider activation completed.");

            return activation;
        }
        catch
        {
            await activation.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Stops only the provider process owned by this activation and drains its output.</summary>
    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
        }
        await _process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15), _clock).ConfigureAwait(false);
        await Task.WhenAll(_stdout, _stderr).ConfigureAwait(false);
        _process.Dispose();
    }
}
