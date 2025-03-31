// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Backchannel;

internal sealed class BackchannelConnectedEvent(IServiceProvider serviceProvider, string socketPath) : IDistributedApplicationEvent
{
    public IServiceProvider Services { get; } = serviceProvider;
    public string SocketPath { get; } = socketPath;
}