// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Components.Common.Tests;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aspire.Npgsql.Tests;

public sealed class PostgreSQLContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new PostgreSqlBuilder()
                .WithImage($"{ContainerImageTags.PostgreSql.Image}:{ContainerImageTags.PostgreSql.Tag}")
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
