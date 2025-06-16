// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Backchannel;

internal interface IBackchannel
{
    Task ConnectAsync(string socketPath, CancellationToken cancellationToken);
    Task<long> PingAsync(long timestamp, CancellationToken cancellationToken);
    Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken);
    string BaselineCapability { get; }
    void CheckCapabilities(string[] capabilities);
}
