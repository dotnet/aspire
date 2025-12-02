// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Encapsulates communication with an AppHost via the auxiliary backchannel.
/// </summary>
internal sealed class AuxiliaryBackchannel : IAuxiliaryBackchannel
{
    private readonly JsonRpc _rpc;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuxiliaryBackchannel"/> class.
    /// </summary>
    /// <param name="rpc">The JSON-RPC connection to the AppHost.</param>
    public AuxiliaryBackchannel(JsonRpc rpc)
    {
        _rpc = rpc ?? throw new ArgumentNullException(nameof(rpc));
    }

    /// <inheritdoc/>
    public async Task<AppHostInformation?> GetAppHostInformationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _rpc.InvokeWithCancellationAsync<AppHostInformation?>("GetAppHostInformationAsync", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log or handle the error as needed
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _rpc.InvokeWithCancellationAsync<DashboardMcpConnectionInfo?>("GetDashboardMcpConnectionInfoAsync", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log or handle the error as needed
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<TestResults?> GetTestResultsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _rpc.InvokeWithCancellationAsync<TestResults?>("GetTestResultsAsync", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log or handle the error as needed
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task StopAppHostAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _rpc.InvokeWithCancellationAsync("StopAppHostAsync", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log or handle the error as needed
            // The AppHost may disconnect before responding, which is expected
        }
    }
}
