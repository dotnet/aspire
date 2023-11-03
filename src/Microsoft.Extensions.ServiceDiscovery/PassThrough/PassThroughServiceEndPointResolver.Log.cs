// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.PassThrough;

internal sealed partial class PassThroughServiceEndPointResolver
{
    private sealed partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Using pass-through service endpoint resolver for service '{ServiceName}'.", EventName = "UsingPassThrough")]
        internal static partial void UsingPassThrough(ILogger logger, string serviceName);
    }
}
