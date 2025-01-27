// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dcp;

internal interface IDcpExecutor
{
    Task RunApplicationAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task StartResourceAsync(string resourceName, CancellationToken cancellationToken);
    Task StopResourceAsync(string resourceName, CancellationToken cancellationToken);
}
