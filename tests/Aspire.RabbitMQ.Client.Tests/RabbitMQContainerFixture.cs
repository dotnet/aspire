// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Testcontainers.RabbitMq;
using Xunit;

namespace Aspire.RabbitMQ.Client.Tests;

public sealed class RabbitMQContainerFixture : IAsyncLifetime
{
    private RabbitMqContainer? _container;

    public string GetConnectionString() => _container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            _container = new RabbitMqBuilder().Build();
            await _container.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
