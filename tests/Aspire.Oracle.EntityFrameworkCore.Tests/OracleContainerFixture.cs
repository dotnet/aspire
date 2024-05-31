// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Xunit;
using Xunit.Abstractions;
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

    public async Task InitializeAsync()
    {
        if (RequiresDockerAttribute.IsSupported)
        {
            Container = new OracleBuilder()
                .WithPortBinding(5432, 1521)
                .WithWaitStrategy(Wait
                    .ForUnixContainer()
                    .UntilMessageIsLogged("Started service freepdb1/freepdb1/freepdb1")
                    .UntilMessageIsLogged("Completed: ALTER DATABASE OPEN")
                    //.AddCustomWaitStrategy(new OracleWaitStrategy(_diagnosticMessageSink))
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
        private readonly IMessageSink _diagnosticMessageSink;

        public OracleWaitStrategy(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public async Task<bool> UntilAsync(IContainer container)
        {
            if (container is OracleContainer oracleContainer)
            {
                while (true)
                {
                    try
                    {
                        var executionResult = await oracleContainer.ExecScriptAsync("SELECT * FROM DUAL");

                        if (executionResult.ExitCode == 0) { return true; }
                        else
                        {
                            _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Failed to SELECT. Exit code {executionResult.ExitCode}. Logs: {await oracleContainer.GetLogsAsync()}"));
                        }
                    }
                    catch (OracleException e)
                    {
                        _diagnosticMessageSink.OnMessage(new DiagnosticMessage($"OracleException ({e.Number}) was thrown. Logs: {await oracleContainer.GetLogsAsync()}"));
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
