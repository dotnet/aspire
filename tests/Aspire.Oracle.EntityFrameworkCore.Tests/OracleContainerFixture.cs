// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.TestUtilities;
using DotNet.Testcontainers.Builders;
using Testcontainers.Oracle;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Oracle.EntityFrameworkCore.Tests;

public sealed class OracleContainerFixture : IAsyncLifetime
{
    private readonly IMessageSink _diagnosticMessageSink;

    public OracleContainer? Container { get; private set; }

    public string GetConnectionString() => Container?.GetConnectionString() ??
        throw new InvalidOperationException("The test container was not initialized.");

    public OracleContainerFixture(IMessageSink diagnosticMessageSink)
    {
        _diagnosticMessageSink = diagnosticMessageSink;
    }

    public async ValueTask InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new OracleBuilder()
                .WithPortBinding(1521, true)
                .WithHostname("localhost")
                .WithImage($"{ComponentTestConstants.AspireTestContainerRegistry}/gvenzl/oracle-xe:21.3.0-slim-faststart")
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilMessageIsLogged("Completed: ALTER DATABASE OPEN")
                ).Build();

            await Container.StartAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Container is not null)
        {
            await Container.DisposeAsync();
        }
    }
}

[CollectionDefinition("Oracle Database collection")]
public class DatabaseCollection : ICollectionFixture<OracleContainerFixture>
{

}
