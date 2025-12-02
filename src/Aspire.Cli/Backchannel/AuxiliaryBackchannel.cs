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
    public Task<AppHostInformation?> GetAppHostInformationAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.InvokeWithCancellationAsync<AppHostInformation?>("GetAppHostInformationAsync", cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<DashboardMcpConnectionInfo?> GetDashboardMcpConnectionInfoAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.InvokeWithCancellationAsync<DashboardMcpConnectionInfo?>("GetDashboardMcpConnectionInfoAsync", cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TestResults?> GetTestResultsAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.InvokeWithCancellationAsync<TestResults?>("GetTestResultsAsync", cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public Task StopAppHostAsync(CancellationToken cancellationToken = default)
    {
        return _rpc.InvokeWithCancellationAsync("StopAppHostAsync", cancellationToken: cancellationToken);
    }
}
