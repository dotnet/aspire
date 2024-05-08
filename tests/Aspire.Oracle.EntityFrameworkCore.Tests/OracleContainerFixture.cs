// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Testcontainers.Oracle;
using Xunit;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public sealed class OracleContainerFixture : IAsyncLifetime
{
    public OracleContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public async Task InitializeAsync()
    {
        if (RequiresDockerTheoryAttribute.IsSupported)
        {
            Container = new OracleFreeBuilder()
                .WithImage($"gvenzl/oracle-free:23-slim-faststart")
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
