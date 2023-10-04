// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Publishing;

internal sealed class DcpPublisher : IDistributedApplicationPublisher
{
    public string Name => "dcp";

    public Task PublishAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
