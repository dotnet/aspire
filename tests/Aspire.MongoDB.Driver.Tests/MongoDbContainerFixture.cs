// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Testcontainers.MongoDb;
using Xunit;

namespace Aspire.MongoDB.Driver.Tests;

public sealed class MongoDbContainerFixture : IAsyncLifetime
{
    public MongoDbContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            // testcontainers uses mongo:mongo by default,
            // resetting that for tests
            Container = new MongoDbBuilder()
                .WithImage("mongo:7.0.5")
                .WithUsername(null)
                .WithPassword(null)
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
