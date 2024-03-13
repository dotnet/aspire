// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Testcontainers.PostgreSql;
using Xunit;

namespace Aspire.Npgsql.Tests;

public sealed class PostgreSQLContainerFixture : IAsyncLifetime
{
    public PostgreSqlContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized. Docker support: {RequiresDockerTheoryAttribute.IsSupported}");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new PostgreSqlBuilder()
                .WithImage("postgres:16.2")
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
