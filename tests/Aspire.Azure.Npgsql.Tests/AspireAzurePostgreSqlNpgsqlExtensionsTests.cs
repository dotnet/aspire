// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Xunit;

namespace Aspire.Azure.Npgsql.Tests;

public class AspireAzurePostgreSqlNpgsqlExtensionsTests
{
    private const string ConnectionString = "Host=localhost;Database=test_aspire_npgsql";
    private const string ConnectionStringWithUsername = "Host=localhost;Database=test_aspire_npgsql;Username=admin";
    private const string ConnectionStringWithUsernameAndPassword = "Host=localhost;Database=test_aspire_npgsql;Username=admin;Password=p@ssw0rd1";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsUsernameFromToken(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials);
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Contains(ConnectionString, dataSource.ConnectionString);
        Assert.Contains("Username=mikey@mouse.com", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsUsernameFromManagedIdentityToken(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var fakeCred = new FakeTokenCredential(useManagedIdentity: true);
        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", configureSettings: settings => settings.Credential = fakeCred);
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", configureSettings: settings => settings.Credential = fakeCred);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Contains(ConnectionString, dataSource.ConnectionString);
        Assert.Contains("Username=mi-123", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TokenCredentialIsIgnoreWhenUsernameAndPasswordAreSet(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionStringWithUsernameAndPassword)
        ]);

        FakeTokenCredential? tokenCredential = null;

        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", configureSettings: settings =>
            {
                ConfigureTokenCredentials(settings);
                tokenCredential = settings.Credential as FakeTokenCredential;
            });
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", configureSettings: settings =>
            {
                ConfigureTokenCredentials(settings);
                tokenCredential = settings.Credential as FakeTokenCredential;
            });
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.NotNull(tokenCredential);
        Assert.Equal(ConnectionStringWithUsername, dataSource.ConnectionString);
        Assert.False(tokenCredential.IsGetTokenInvoked);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", "unused")
        ]);

        static void SetConnectionString(NpgsqlSettings settings) => settings.ConnectionString = ConnectionStringWithUsernameAndPassword;

        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", SetConnectionString);
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", SetConnectionString);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Equal(ConnectionStringWithUsername, dataSource.ConnectionString);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "npgsql" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Npgsql", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionStringWithUsername)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials);
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.Equal(ConnectionStringWithUsername, dataSource.ConnectionString);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", dataSource.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CustomDataSourceBuilderIsExecuted(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql", ConnectionString)
        ]);

        var wasCalled = false;
        void configureDataSourceBuilder(NpgsqlDataSourceBuilder b) => wasCalled = true;

        if (useKeyed)
        {
            builder.AddKeyedAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials, configureDataSourceBuilder: configureDataSourceBuilder);
        }
        else
        {
            builder.AddAzureNpgsqlDataSource("npgsql", configureSettings: ConfigureTokenCredentials, configureDataSourceBuilder: configureDataSourceBuilder);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql") :
            host.Services.GetRequiredService<NpgsqlDataSource>();

        Assert.True(wasCalled);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql1", "Host=localhost1;Database=test_aspire_npgsql"),
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql2", "Host=localhost2;Database=test_aspire_npgsql"),
            new KeyValuePair<string, string?>("ConnectionStrings:npgsql3", "Host=localhost3;Database=test_aspire_npgsql"),
        ]);

        builder.AddAzureNpgsqlDataSource("npgsql1", configureSettings: ConfigureTokenCredentials);
        builder.AddKeyedAzureNpgsqlDataSource("npgsql2", configureSettings: ConfigureTokenCredentials);
        builder.AddKeyedAzureNpgsqlDataSource("npgsql3", configureSettings: ConfigureTokenCredentials);

        using var host = builder.Build();

        var connection1 = host.Services.GetRequiredService<NpgsqlDataSource>();
        var connection2 = host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql2");
        var connection3 = host.Services.GetRequiredKeyedService<NpgsqlDataSource>("npgsql3");

        Assert.NotSame(connection1, connection2);
        Assert.NotSame(connection1, connection3);
        Assert.NotSame(connection2, connection3);

        Assert.Contains("localhost1", connection1.ConnectionString);
        Assert.Contains("localhost2", connection2.ConnectionString);
        Assert.Contains("localhost3", connection3.ConnectionString);
    }

    private void ConfigureTokenCredentials(AzureNpgsqlSettings settings)
    {
        settings.Credential = new FakeTokenCredential();
    }
}
