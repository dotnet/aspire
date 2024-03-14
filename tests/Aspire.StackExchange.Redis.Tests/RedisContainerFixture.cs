// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common;
using Aspire.Components.Common.Tests;
using Testcontainers.Redis;
using Xunit;

namespace Aspire.StackExchange.Redis.Tests;

public sealed class RedisContainerFixture : IAsyncLifetime
{
    public RedisContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new RedisBuilder()
                            .WithImage($"{ContainerImageTags.Redis.Image}:{ContainerImageTags.Redis.Tag}")
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
