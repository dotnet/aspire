// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Tests.Helpers;
internal sealed class NoopPublisher(IHostApplicationLifetime lifetime) : IDistributedApplicationPublisher
{
    private readonly IHostApplicationLifetime _lifetime = lifetime;

    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        _lifetime.StopApplication();
        return Task.CompletedTask;
    }
}
