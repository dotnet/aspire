// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.MySql;
using Testcontainers.MySql;
using Xunit;

namespace Aspire.MySqlConnector.Tests;

public sealed class MySqlContainerFixture : IAsyncLifetime
{
    public MySqlContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new MySqlBuilder()
                .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/{MySqlContainerImageTags.Image}:{MySqlContainerImageTags.Tag}")
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
