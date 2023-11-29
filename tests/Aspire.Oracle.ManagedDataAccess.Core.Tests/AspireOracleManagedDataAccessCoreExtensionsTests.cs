// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace Aspire.Oracle.ManagedDataAccess.Core.Tests;

public class AspireOracleManagedDataAccessCoreExtensionsTests
{
    private const string ConnectionString = "user id=system;password=password;data source=localhost:port/freepdb1";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orcl", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOracleManagedDataAccessCore("orcl");
        }
        else
        {
            builder.AddOracleManagedDataAccessCore("orcl");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<OracleConnection>("orcl") :
            host.Services.GetRequiredService<OracleConnection>();

        Assert.Equal(ConnectionString, connection.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:orcl", "unused")
        ]);

        static void SetConnectionString(OracleManagedDataAccessCoreSettings settings) => settings.ConnectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedOracleManagedDataAccessCore("orcl", SetConnectionString);
        }
        else
        {
            builder.AddOracleManagedDataAccessCore("orcl", SetConnectionString);
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<OracleConnection>("orcl") :
            host.Services.GetRequiredService<OracleConnection>();

        Assert.Equal(ConnectionString, connection.ConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", connection.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "orcl" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Oracle:ManagedDataAccess:Core", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:orcl", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOracleManagedDataAccessCore("orcl");
        }
        else
        {
            builder.AddOracleManagedDataAccessCore("orcl");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<OracleConnection>("orcl") :
            host.Services.GetRequiredService<OracleConnection>();

        Assert.Equal(ConnectionString, connection.ConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", connection.ConnectionString);
    }
}
