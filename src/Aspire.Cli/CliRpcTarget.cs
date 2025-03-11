// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Cli;

internal class CliRpcTarget(Logger<CliRpcTarget> logger)
{
    public Task<long> PingAsync(long timestamp)
    {
        logger.LogTrace("Received ping from AppHost with timestamp: {Timestamp}", timestamp);
        return Task.FromResult(timestamp);
    }
}