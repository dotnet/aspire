// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using Aspire.TestUtilities;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using MySqlConnector;
using Polly;
using Xunit;

namespace Aspire.Hosting.MySql.Tests;

public class MySqlFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private static readonly Predicate<string> s_mySqlReadyText = log => log.Contains("ready for connections") && log.Contains("port: 3306");

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnMySqlBlocksDependentResources()
    {
        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan);
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddMySql("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddMySql("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync(cts.Token);
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyMySqlResource()
    {
        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2), ShouldHandle = new PredicateBuilder().Handle<MySqlException>() })
            .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var mySqlDbName = "db1";

        var mysql = builder.AddMySql("mysql").WithEnvironment("MYSQL_DATABASE", mySqlDbName);
        var db = mysql.AddDatabase(mySqlDbName);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddMySqlDataSource(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync(cts.Token);
        await pipeline.ExecuteAsync(async token =>
        {
            using var connection = host.Services.GetRequiredService<MySqlConnection>();
            await connection.OpenAsync(token);

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1";
            var results = await command.ExecuteReaderAsync(token);

            Assert.True(results.HasRows);
        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var mySqlDbName = "tempdb";

        string? volumeName = null;
        string? bindMountPath = null;

        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var mysql1 = builder1.AddMySql("mysql").WithEnvironment("MYSQL_DATABASE", mySqlDbName);
            var password = mysql1.Resource.PasswordParameter.Value;

            var db1 = mysql1.AddDatabase(mySqlDbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.Generate(mysql1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                mysql1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;

                mysql1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync(cts.Token);

                await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddMySqlDataSource(db1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync(cts.Token);

                        // Wait until the database is available
                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<MySqlConnection>();
                            await connection.OpenAsync(token);
                            Assert.Equal(ConnectionState.Open, connection.State);
                        }, cts.Token);

                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<MySqlConnection>();
                            await connection.OpenAsync(token);

                            var command = connection.CreateCommand();
                            command.CommandText = """
                                CREATE TABLE cars (brand VARCHAR(255));
                                INSERT INTO cars (brand) VALUES ('BatMobile');
                                SELECT * FROM cars;
                            """;

                            var results = await command.ExecuteReaderAsync(token);

                            Assert.True(results.HasRows);
                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync(cts.Token);
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var passwordParameter2 = builder2.AddParameter("pwd", password);

            var mysql2 = builder2.AddMySql("mysql", passwordParameter2);
            var db2 = mysql2.AddDatabase(mySqlDbName);

            if (useVolume)
            {
                mysql2.WithDataVolume(volumeName);
            }
            else
            {
                mysql2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync(cts.Token);

                await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(cts.Token)
                    });

                    hb.AddMySqlDataSource(db2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync(cts.Token);

                        // Wait until the database is available
                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<MySqlConnection>();
                            await connection.OpenAsync(token);
                            Assert.Equal(ConnectionState.Open, connection.State);
                        }, cts.Token);

                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<MySqlConnection>();
                            await connection.OpenAsync(token);

                            var command = connection.CreateCommand();
                            command.CommandText = $"SELECT * FROM cars;";
                            var results = await command.ExecuteReaderAsync(token);

                            Assert.True(results.HasRows);
                        }, cts.Token);
                    }

                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync(cts.Token);
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
                    Directory.Delete(bindMountPath);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithInitBindMount()
    {
        // Creates a script that should be executed when the container is initialized.

        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2), ShouldHandle = new PredicateBuilder().Handle<MySqlException>() })
            .Build();

        var bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(bindMountPath);

        try
        {
            File.WriteAllText(Path.Combine(bindMountPath, "init.sql"), """
                CREATE TABLE cars (brand VARCHAR(255));
                INSERT INTO cars (brand) VALUES ('BatMobile');
            """);

            using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var mySqlDbName = "db1";

            var mysql = builder.AddMySql("mysql").WithEnvironment("MYSQL_DATABASE", mySqlDbName);
            var db = mysql.AddDatabase(mySqlDbName);

#pragma warning disable CS0618 // Type or member is obsolete
            mysql.WithInitBindMount(bindMountPath);
#pragma warning restore CS0618 // Type or member is obsolete

            using var app = builder.Build();

            await app.StartAsync(cts.Token);

            await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(cts.Token)
            });

            hb.AddMySqlDataSource(db.Resource.Name);

            using var host = hb.Build();

            await host.StartAsync(cts.Token);

            // Wait until the database is available
            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<MySqlConnection>();
                await connection.OpenAsync(token);
                Assert.Equal(ConnectionState.Open, connection.State);
            }, cts.Token);

            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<MySqlConnection>();
                await connection.OpenAsync(token);

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM cars;";

                var results = await command.ExecuteReaderAsync(token);
                Assert.True(await results.ReadAsync(token));
                Assert.Equal("BatMobile", results.GetString("brand"));
                Assert.False(await results.ReadAsync(token));
            }, cts.Token);
        }
        finally
        {
            try
            {
                Directory.Delete(bindMountPath);
            }
            catch
            {
                // Don't fail test if we can't clean the temporary folder
            }
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithInitFiles()
    {
        // Creates a script that should be executed when the container is initialized.

        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2), ShouldHandle = new PredicateBuilder().Handle<MySqlException>() })
            .Build();

        var initFilesPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        Directory.CreateDirectory(initFilesPath);

        try
        {
            File.WriteAllText(Path.Combine(initFilesPath, "init.sql"), """
                CREATE TABLE cars (brand VARCHAR(255));
                INSERT INTO cars (brand) VALUES ('BatMobile');
            """);

            using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var mySqlDbName = "db1";

            var mysql = builder.AddMySql("mysql").WithEnvironment("MYSQL_DATABASE", mySqlDbName);
            var db = mysql.AddDatabase(mySqlDbName);

            mysql.WithInitFiles(initFilesPath);

            using var app = builder.Build();

            await app.StartAsync(cts.Token);

            await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(cts.Token)
            });

            hb.AddMySqlDataSource(db.Resource.Name);

            using var host = hb.Build();

            await host.StartAsync(cts.Token);

            // Wait until the database is available
            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<MySqlConnection>();
                await connection.OpenAsync(token);
                Assert.Equal(ConnectionState.Open, connection.State);
            }, cts.Token);

            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<MySqlConnection>();
                await connection.OpenAsync(token);

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM cars;";

                var results = await command.ExecuteReaderAsync(token);
                Assert.True(await results.ReadAsync(token));
                Assert.Equal("BatMobile", results.GetString("brand"));
                Assert.False(await results.ReadAsync(token));
            }, cts.Token);
        }
        finally
        {
            try
            {
                Directory.Delete(initFilesPath);
            }
            catch
            {
                // Don't fail test if we can't clean the temporary folder
            }
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyEfMySql()
    {
        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<MySqlException>() })
            .Build();

        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var mySqlDbName = "db1";

        var mysql = builder.AddMySql("mysql").WithEnvironment("MYSQL_DATABASE", mySqlDbName);
        var db = mysql.AddDatabase(mySqlDbName);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        await app.WaitForTextAsync(s_mySqlReadyText, cts.Token).WaitAsync(cts.Token);

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(cts.Token)
        });

        hb.AddMySqlDbContext<TestDbContext>(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync(cts.Token);

        // Wait until the database is available
        await pipeline.ExecuteAsync(async token =>
        {
            var dbContext = host.Services.GetRequiredService<TestDbContext>();
            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
            Assert.True(await databaseCreator.CanConnectAsync(token));
        }, cts.Token);

        // Initialize database schema
        await pipeline.ExecuteAsync(async token =>
        {
            var dbContext = host.Services.GetRequiredService<TestDbContext>();
            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
            await databaseCreator.CreateTablesAsync(token);
        }, cts.Token);

        await pipeline.ExecuteAsync(async token =>
        {
            var dbContext = host.Services.GetRequiredService<TestDbContext>();
            dbContext.Cars.Add(new TestDbContext.Car { Brand = "BatMobile" });
            await dbContext.SaveChangesAsync(token);
        }, cts.Token);

        await pipeline.ExecuteAsync(async token =>
        {
            var dbContext = host.Services.GetRequiredService<TestDbContext>();
            var cars = await dbContext.Cars.ToListAsync(token);
            Assert.Single(cars);
            Assert.Equal("BatMobile", cars[0].Brand);
        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task MySql_WithPersistentLifetime_ReusesContainers(bool useMultipleInstances)
    {
        // When WithPhpMyAdmin in invoked with 2 and two or more MySql instances are created,
        // it generates and mounts a config.user.inc.php file instead of using environment variables.
        // For this reason we need to test with and without multiple instances to cover both scenarios.

        using var cts = new CancellationTokenSource(TestConstants.ExtraLongTimeoutTimeSpan * 2);

        // Use the same path for both runs
        var aspireStorePath = Directory.CreateTempSubdirectory().FullName;

        var before = await RunContainersAsync();
        var after = await RunContainersAsync();

        Assert.All(before, Assert.NotNull);
        Assert.All(after, Assert.NotNull);
        Assert.Equal(before, after);

        try
        {
            Directory.Delete(aspireStorePath, true);
        }
        catch
        {
            // Don't fail test if we can't clean the temporary folder
        }

        async Task<string?[]> RunContainersAsync()
        {
            using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper)
                .WithTempAspireStore(aspireStorePath)
                .WithResourceCleanUp(false);

            var passwordParameter = builder.AddParameter("pwd", "p@ssw0rd1", secret: true);
            var mysql = builder
                .AddMySql("resource", password: passwordParameter).WithLifetime(ContainerLifetime.Persistent)
                .WithPhpMyAdmin(c => c.WithLifetime(ContainerLifetime.Persistent))
                .AddDatabase("db");

            if (useMultipleInstances)
            {
                var passwordParameter2 = builder.AddParameter("pwd2", "p@ssw0rd2", secret: true);
                builder.AddMySql("resource2", password: passwordParameter2).WithLifetime(ContainerLifetime.Persistent);
            }

            var app = builder.Build();
            await app.StartAsync(cts.Token);

            var rns = app.Services.GetRequiredService<ResourceNotificationService>();

            var resourceEvent = await rns.WaitForResourceHealthyAsync("resource", cts.Token);
            var mySqlId = GetContainerId(resourceEvent);

            var mySqlId2 = "";

            if (useMultipleInstances)
            {
                resourceEvent = await rns.WaitForResourceHealthyAsync("resource2", cts.Token);
                mySqlId2 = GetContainerId(resourceEvent);
            }

            resourceEvent = await rns.WaitForResourceHealthyAsync("resource-phpmyadmin", cts.Token);
            var phpMyAdminId = GetContainerId(resourceEvent);

            await app.StopAsync(cts.Token).WaitAsync(cts.Token);

            return [mySqlId, mySqlId2, phpMyAdminId];
        }

        static string? GetContainerId(ResourceEvent resourceEvent)
        {
            return resourceEvent.Snapshot.Properties.FirstOrDefault(x => x.Name == "container.id")?.Value?.ToString();
        }
    }
}
