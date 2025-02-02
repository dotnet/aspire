// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Nats;
using Aspire.Components.Common.Tests;
using Testcontainers.Nats;
using Xunit;

namespace Aspire.NATS.Net.Tests;

public sealed class NatsContainerFixture : IAsyncLifetime
{
    public NatsContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new NatsBuilder()
                .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{NatsContainerImageTags.Image}:{NatsContainerImageTags.Tag}")
                .Build();
            await Container.StartAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}
