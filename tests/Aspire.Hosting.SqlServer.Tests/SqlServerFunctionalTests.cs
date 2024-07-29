// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.SqlServer.Tests;

public class SqlServerFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string TestDbName = "testdb";
    private const string MasterDbName = "master";

    [Fact]
    [RequiresDocker]
    public async Task VerifySqlServerResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
            .Build();

        var builder = CreateDistributedApplicationBuilder();

        var sqlserver = builder.AddSqlServer("sqlserver");
        var testDb = sqlserver.AddDatabase(TestDbName);
        var masterDb = sqlserver.AddDatabase(MasterDbName);
        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{testDb.Resource.Name}"] = await testDb.Resource.ConnectionStringExpression.GetValueAsync(default),
            [$"ConnectionStrings:{masterDb.Resource.Name}"] = await masterDb.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddKeyedSqlServerClient(masterDb.Resource.Name);
        hb.AddSqlServerClient(testDb.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            var masterDbConnection = host.Services.GetRequiredKeyedService<SqlConnection>(masterDb.Resource.Name);
            var testDbConnection = host.Services.GetRequiredService<SqlConnection>();

            await CreateTestDb(masterDbConnection, testDbConnection, token);

        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    //[SkipOnCI("https://github.com/dotnet/aspire/issues/5055")]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {

        string? volumeName = null;
        string? bindMountPath = null;

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(10) })
            .Build();

        try
        {
            var builder1 = CreateDistributedApplicationBuilder();

            var password = "p@ssw0rd1";

            var passwordParameter = builder1.AddParameter("pwd");
            builder1.Configuration["Parameters:pwd"] = password;

            var sqlserver1 = builder1.AddSqlServer("sqlserver", passwordParameter);
            var testdb1 = sqlserver1.AddDatabase(TestDbName);
            var masterDb1 = sqlserver1.AddDatabase(MasterDbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(sqlserver1, nameof(WithDataShouldPersistStateBetweenUsages));

                // If the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                sqlserver1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(bindMountPath);
                sqlserver1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{testdb1.Resource.Name}"] = await testdb1.Resource.ConnectionStringExpression.GetValueAsync(default),
                        [$"ConnectionStrings:{masterDb1.Resource.Name}"] = await masterDb1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddKeyedSqlServerClient(masterDb1.Resource.Name);
                    hb.AddSqlServerClient(testdb1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {

                            var masterDbConnection = host.Services.GetRequiredKeyedService<SqlConnection>(masterDb1.Resource.Name);
                            var testDbConnection = host.Services.GetRequiredService<SqlConnection>();
                            await CreateTestDb(masterDbConnection, testDbConnection, token);

                            if (testDbConnection.State != System.Data.ConnectionState.Open )
                            {
                                await testDbConnection.OpenAsync(token);
                            }

                            var command = testDbConnection.CreateCommand();
                            command.CommandText = """
                                DROP TABLE IF EXISTS Cars
                                CREATE TABLE Cars (Brand VARCHAR(255));
                                INSERT INTO Cars (Brand) VALUES ('BatMobile');
                                SELECT * FROM Cars;
                            """;

                            var results = await command.ExecuteReaderAsync(token);

                            Assert.True(results.HasRows);
                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            var builder2 = CreateDistributedApplicationBuilder();
            passwordParameter = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:pwd"] = password;

            var sqlserver2 = builder2.AddSqlServer("sqlserver", passwordParameter);
            var testdb2 = sqlserver2.AddDatabase(TestDbName);

            if (useVolume)
            {
                sqlserver2.WithDataVolume(volumeName);
            }
            else
            {
                sqlserver2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{testdb2.Resource.Name}"] = await testdb2.Resource.ConnectionStringExpression.GetValueAsync(default),
                    });

                    hb.AddSqlServerClient(testdb2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var testDbConnection = host.Services.GetRequiredService<SqlConnection>();
                            if (testDbConnection.State == System.Data.ConnectionState.Closed)
                            {
                                await testDbConnection.OpenAsync(token);
                            }
                            var command = testDbConnection.CreateCommand();
                            command.CommandText = $"SELECT * FROM cars;";
                            var results = await command.ExecuteReaderAsync(token);

                            Assert.True(await results.ReadAsync(token));
                            Assert.Equal("BatMobile", results.GetString(0));
                            Assert.False(await results.ReadAsync(token));
                            await testDbConnection.CloseAsync();
                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

        }
        finally
        {
            if (volumeName is not null)
            {
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
            }

            if (bindMountPath is not null)
            {
                try
                {
                    Directory.Delete(bindMountPath, recursive: true);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    private static async Task CreateTestDb(SqlConnection masterDbConnection, SqlConnection testDbConnection, CancellationToken token)
    {
        if (masterDbConnection.State != System.Data.ConnectionState.Open)
        {
            await masterDbConnection.OpenAsync(token);
        }

        //drop database first previous test or command failed
        var createCommand = masterDbConnection.CreateCommand();
        createCommand.CommandText =
            $"""
                DROP DATABASE IF EXISTS {TestDbName}
                Create Database {TestDbName}
             """;
        await createCommand.ExecuteNonQueryAsync(token);

        if (testDbConnection.State != System.Data.ConnectionState.Open)
        {
            await testDbConnection.OpenAsync(token);
        }

        var selectCommand = testDbConnection.CreateCommand();
        selectCommand.CommandText = $"SELECT 1";
        var results = await selectCommand.ExecuteReaderAsync(token);

        Assert.True(results.HasRows);
        //close connection otherwise drop database failed
        await masterDbConnection.CloseAsync();
        await testDbConnection.CloseAsync();
    }

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddXunitLogging(testOutputHelper);
        builder.Services.AddHostedService<ResourceLoggerForwarderService>();

        return builder;
    }
}
