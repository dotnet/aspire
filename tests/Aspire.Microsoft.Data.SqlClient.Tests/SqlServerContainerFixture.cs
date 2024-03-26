// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting;
using Testcontainers.MsSql;
using Xunit;

namespace Aspire.Microsoft.Data.SqlClient.Tests;

public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    public MsSqlContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new MsSqlBuilder()
                            .WithImage($"{SqlServerContainerImageTags.Registry}/{SqlServerContainerImageTags.Image}:{SqlServerContainerImageTags.Tag}")
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
