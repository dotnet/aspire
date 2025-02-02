// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Redis;
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
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = await CreateContainerAsync();
        }
    }

    public async Task DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }

    public static async Task<RedisContainer> CreateContainerAsync()
    {
        var container = new RedisBuilder()
            .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{RedisContainerImageTags.Image}:{RedisContainerImageTags.Tag}")
            .Build();
        await container.StartAsync();

        return container;
    }
}
