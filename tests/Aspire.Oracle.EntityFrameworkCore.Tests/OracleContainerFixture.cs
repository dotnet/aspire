// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Oracle.ManagedDataAccess.Client;
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
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new OracleBuilder()
                .WithPortBinding(1521, 1521)
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilMessageIsLogged("Started service freepdb1/freepdb1/freepdb1")
                    .UntilMessageIsLogged("Completed: ALTER DATABASE OPEN")
                    .AddCustomWaitStrategy(new OracleWaitStrategy())
                )
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

    public class OracleWaitStrategy : IWaitUntil
    {
        public async Task<bool> UntilAsync(IContainer container)
        {
            if (container is OracleContainer oracleContainer)
            {
                while(true)
                {
                    try
                    {
                        var executionResult = await oracleContainer.ExecScriptAsync("SELECT * FROM DUAL");

                        if(executionResult.ExitCode == 0)
                        {
                            return true;
                        }
                    }
                    catch (OracleException)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
            else
            {
                Assert.Fail("The container is not an OracleContainer");
                return true;
            }
        }
    }
}

[CollectionDefinition("Oracle Database collection")]
public class DatabaseCollection : ICollectionFixture<OracleContainerFixture>
{

}
