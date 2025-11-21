// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Backchannel;

/// <summary>
/// Event that is published when a client connects to the auxiliary backchannel.
/// </summary>
/// <remarks>
/// The auxiliary backchannel supports multiple concurrent connections, so this event
/// may be published multiple times during the lifetime of the application.
/// </remarks>
internal sealed class AuxiliaryBackchannelConnectedEvent(IServiceProvider serviceProvider, string socketPath) : IDistributedApplicationEvent
{
    /// <summary>
    /// Gets the service provider for the application.
    /// </summary>
    public IServiceProvider Services { get; } = serviceProvider;

    /// <summary>
    /// Gets the Unix socket path where the auxiliary backchannel is listening.
    /// </summary>
    public string SocketPath { get; } = socketPath;
}
