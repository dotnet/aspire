// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Oracle.Tests;

public class OracleFunctionalTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public OracleFunctionalTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyOracleResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        //var pipeline = new ResiliencePipelineBuilder()
        //    .AddRetry(new() {
        //        MaxRetryAttempts = int.MaxValue,
        //        BackoffType = DelayBackoffType.Linear,
        //        ShouldHandle = new PredicateBuilder().HandleResult(false),
        //        Delay = TimeSpan.FromSeconds(2) })
        //    .Build();

        var builder = CreateDistributedApplicationBuilder();

        var oracleDbName = "freepdb1";

        var oracle = builder.AddOracle("oracle");
        ConfigureTestOracleDatabase(oracle);

        oracle.WithEnvironment("ORACLE_PASSWORD", oracle.Resource.PasswordParameter.Value);

        var db = oracle.AddDatabase(oracleDbName);

        var ready = builder.WaitForText(oracle.Resource, "Completed: ALTER DATABASE OPEN", cts.Token);

        using var app = builder.Build();

        await app.StartAsync(cts.Token);
        await ready.WaitAsync(TimeSpan.FromMinutes(2));

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddOracleDatabaseDbContext<TestDbContext>(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        using var dbContext = host.Services.GetRequiredService<TestDbContext>();
        var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();

        try
        {
            await databaseCreator.CanConnectAsync(cts.Token);
        }
        catch
        {
            // Ignore failures
        }

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "/usr/bin/env",
            Arguments = "bash docker exec $(docker ps -l -q) bash -c \"cat /opt/oracle/diag/rdbms/free/FREE/trace/*.trc & cat /opt/oracle/diag/rdbms/free/FREE/trace/*.log\"",
            RedirectStandardOutput = true,
        });

        process!.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
        {
            _testOutputHelper.WriteLine($"[DOCKER] {e.Data}");
        };

        process.Start();
        process.WaitForExit();
    }

    [Theory]
    [InlineData(true, Skip = " Debugging")]
    [InlineData(false, Skip = "Takes too long to be ready.")]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var oracleDbName = "freepdb1";

        string? volumeName = null;
        string? bindMountPath = null;

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = new PredicateBuilder().HandleResult(false),
                Delay = TimeSpan.FromSeconds(2)
            })
            .Build();

        try
        {
            var builder1 = CreateDistributedApplicationBuilder();

            var oracle1 = builder1.AddOracle("oracle");
            ConfigureTestOracleDatabase(oracle1);

            var password = oracle1.Resource.PasswordParameter.Value;

            var db1 = oracle1.AddDatabase(oracleDbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(oracle1, nameof(WithDataShouldPersistStateBetweenUsages));

                // If the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                oracle1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(bindMountPath);

                oracle1.WithDataBindMount(bindMountPath);
            }

            var ready1 = builder1.WaitForText(oracle1.Resource, "Completed: ALTER DATABASE OPEN", cts.Token);

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                await ready1.WaitAsync(TimeSpan.FromMinutes(2));

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddOracleDatabaseDbContext<TestDbContext>(db1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        // Wait until the database is available
                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var dbContext = host.Services.GetRequiredService<TestDbContext>();
                            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                            return await databaseCreator.CanConnectAsync(token);
                        }, cts.Token);

                        // Create tables
                        using var dbContext = host.Services.GetRequiredService<TestDbContext>();
                        var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                        await databaseCreator.CreateTablesAsync(cts.Token);

                        // Seed database
                        dbContext.Cars.Add(new TestDbContext.Car { Brand = "BatMobile" });
                        await dbContext.SaveChangesAsync(cts.Token);

                        // Stops the container and wait until it's not accessible anymore before creating a new one
                        // using the same volume.

                        await app.StopAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var dbContext = host.Services.GetRequiredService<TestDbContext>();
                            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();

                            return !await databaseCreator.CanConnectAsync(token);
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
            var passwordParameter2 = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:pwd"] = password;

            var oracle2 = builder2.AddOracle("oracle", passwordParameter2);
            ConfigureTestOracleDatabase(oracle2);

            var db2 = oracle2.AddDatabase(oracleDbName);

            var ready2 = builder2.WaitForText(oracle2.Resource, "Completed: ALTER DATABASE OPEN", cts.Token);

            if (useVolume)
            {
                oracle2.WithDataVolume(volumeName);
            }
            else
            {
                oracle2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                await ready2.WaitAsync(TimeSpan.FromMinutes(2));

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddOracleDatabaseDbContext<TestDbContext>(db2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        // Wait until the database is available
                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var dbContext = host.Services.GetRequiredService<TestDbContext>();
                            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                            return await databaseCreator.CanConnectAsync(token);
                        });

                        using var dbContext = host.Services.GetRequiredService<TestDbContext>();
                        var brands = await dbContext.Cars.ToListAsync(cancellationToken: cts.Token);
                        Assert.Single(brands);

                        await app.StopAsync();

                        // Wait for the database to not be available before attempting to clean the volume.

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var dbContext = host.Services.GetRequiredService<TestDbContext>();
                            var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                            return !await databaseCreator.CanConnectAsync(token);
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
                    Directory.Delete(bindMountPath, true);
                }
                catch
                {
                    // Don't fail test if we can't clean the temporary folder
                }
            }
        }
    }

    [Theory]
    [InlineData(true, Skip = " Debugging")]
    [InlineData(false, Skip = "Scripts are not executed when in the /setup folder")]
    [RequiresDocker]
    public async Task VerifyWithInitBindMount(bool init)
    {
        // Creates a script that should be executed when the container is initialized.

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new()
            {
                MaxRetryAttempts = int.MaxValue,
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = new PredicateBuilder().HandleResult(false),
                Delay = TimeSpan.FromSeconds(2)
            })
            .Build();

        var bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(bindMountPath);

        var oracleDbName = "freepdb1";

        try
        {
            File.WriteAllText(Path.Combine(bindMountPath, "01_init.sql"), $"""
                ALTER SESSION SET CONTAINER={oracleDbName};
                ALTER SESSION SET CURRENT_SCHEMA = SYSTEM;
                CREATE TABLE "Cars" ("Id" NUMBER(10) GENERATED BY DEFAULT ON NULL AS IDENTITY NOT NULL, "Brand" NVARCHAR2(2000) NOT NULL, CONSTRAINT "PK_Cars" PRIMARY KEY ("Id") );
                INSERT INTO "Cars" ("Id", "Brand") VALUES (1, 'BatMobile');
                COMMIT;
            """);

            var builder = CreateDistributedApplicationBuilder();

            var oracle = builder.AddOracle("oracle");
            var db = oracle.AddDatabase(oracleDbName);

            var ready = builder.WaitForText(oracle.Resource, "Completed: ALTER DATABASE OPEN", cts.Token);

            if (init)
            {
                oracle.WithInitBindMount(bindMountPath);
            }
            else
            {
                oracle.WithDbSetupBindMount(bindMountPath);
            }

            using var app = builder.Build();

            await app.StartAsync();

            await ready.WaitAsync(TimeSpan.FromMinutes(2));

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
            });

            hb.AddOracleDatabaseDbContext<TestDbContext>(db.Resource.Name);

            using var host = hb.Build();

            try
            {
                await host.StartAsync();

                // Wait until the database is available
                await pipeline.ExecuteAsync(async token =>
                {
                    var dbContext = host.Services.GetRequiredService<TestDbContext>();
                    var databaseCreator = (RelationalDatabaseCreator)dbContext.Database.GetService<IDatabaseCreator>();
                    return await databaseCreator.CanConnectAsync(token);
                }, cts.Token);

                var dbContext = host.Services.GetRequiredService<TestDbContext>();

                var brands = await dbContext.Cars.ToListAsync(cancellationToken: cts.Token);
                Assert.Single(brands);
                Assert.Equal("BatMobile", brands[0].Brand);
            }
            finally
            {
                await app.StopAsync();
            }
        }
        finally
        {
            try
            {
                Directory.Delete(bindMountPath, true);
            }
            catch
            {
                // Don't fail test if we can't clean the temporary folder
            }
        }
    }

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        // Do not use the test container registry as Oracle has their own
        var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(config => config.SetMinimumLevel(LogLevel.Information));
        builder.Services.AddXunitLogging(_testOutputHelper);
        builder.Services.AddHostedService<ResourceLoggerForwarderService>();
        return builder;
    }

    private static IResourceBuilder<OracleDatabaseServerResource> ConfigureTestOracleDatabase(IResourceBuilder<OracleDatabaseServerResource> oracle)
    {
        return oracle
            .WithImage("gvenzl/oracle-free", "23-slim-faststart")
            .WithImageRegistry("docker.io")
            .WithEnvironment("ORACLE_PASSWORD", oracle.Resource.PasswordParameter.Value)
            ;
    }
}
