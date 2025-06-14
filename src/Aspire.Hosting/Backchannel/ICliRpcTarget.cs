// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Backchannel;

internal interface ICliRpcTarget
{
    Task SendCommandOutputAsync(string output, CancellationToken cancellationToken);
    Task SendCommandErrorAsync(string error, CancellationToken cancellationToken);
}
