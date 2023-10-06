// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.Data.SqlClient.Tests;

public class AspireSqlServerSqlClientExtensionsTests
{
    private const string ConnectionString = "Data Source=fake;Database=master";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sqlconnection", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedSqlServerClient("sqlconnection");
        }
        else
        {
            builder.AddSqlServerClient("sqlconnection");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<SqlConnection>("sqlconnection") :
            host.Services.GetRequiredService<SqlConnection>();

        Assert.Equal(ConnectionString, connection.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sqlconnection", "unused")
        ]);

        static void SetConnectionString(MicrosoftDataSqlClientSettings settings) => settings.ConnectionString = ConnectionString;
        if (useKeyed)
        {
            builder.AddKeyedSqlServerClient("sqlconnection", SetConnectionString);
        }
        else
        {
            builder.AddSqlServerClient("sqlconnection", SetConnectionString);
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<SqlConnection>("sqlconnection") :
            host.Services.GetRequiredService<SqlConnection>();

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

        var key = useKeyed ? "sqlconnection" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Microsoft:Data:SqlClient", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:sqlconnection", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedSqlServerClient("sqlconnection");
        }
        else
        {
            builder.AddSqlServerClient("sqlconnection");
        }

        var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<SqlConnection>("sqlconnection") :
            host.Services.GetRequiredService<SqlConnection>();

        Assert.Equal(ConnectionString, dataSource.ConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }
}
