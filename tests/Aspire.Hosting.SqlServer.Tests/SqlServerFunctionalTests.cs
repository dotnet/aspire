// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.SqlServer.Tests;

public class SqlServerFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnSqlServerBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddSqlServer("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddSqlServer("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await app.ResourceNotifications.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await app.ResourceNotifications.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifySqlServerResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = int.MaxValue, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        using var builder = TestDistributedApplicationBuilder.Create(o => { }, testOutputHelper);

        var sqlserver = builder.AddSqlServer("sqlserver");
        var tempDb = sqlserver.AddDatabase("tempdb");

        using var app = builder.Build();

        await app.StartAsync(cts.Token);

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{tempDb.Resource.Name}"] = await tempDb.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddSqlServerDbContext<TestDbContext>(tempDb.Resource.Name);
        hb.AddSqlServerClient(tempDb.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        // Test SqlConnection
        await pipeline.ExecuteAsync(async token =>
        {
            var conn = host.Services.GetRequiredService<SqlConnection>();

            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(token);
            }

            var selectCommand = conn.CreateCommand();
            selectCommand.CommandText = $"SELECT 1";
            var results = await selectCommand.ExecuteReaderAsync(token);

            Assert.True(results.HasRows);
        }, cts.Token);

        var dbContext = host.Services.GetRequiredService<TestDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cts.Token);
        dbContext.Cars.Add(new TestDbContext.Car { Brand = "BatMobile" });
        await dbContext.SaveChangesAsync(cts.Token);
        var cars = await dbContext.Cars.ToListAsync(cts.Token);

        Assert.Single(cars);
        Assert.Equal("BatMobile", cars[0].Brand);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        string? volumeName = null;
        string? bindMountPath = null;

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = int.MaxValue, BackoffType = DelayBackoffType.Linear, Delay = TimeSpan.FromSeconds(2) })
            .Build();

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.Create(o => { }, testOutputHelper);

            var sqlserver1 = builder1.AddSqlServer("sqlserver");
            var masterdb1 = sqlserver1.AddDatabase("master");

            var password = sqlserver1.Resource.PasswordParameter.Value;

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.Generate(sqlserver1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                sqlserver1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                Directory.CreateDirectory(bindMountPath);

                if (!OperatingSystem.IsWindows())
                {
                    // The docker container runs as a non-root user, so we need to grant other user's read/write permission
                    // to the bind mount directory.
                    // Note that we need to do this after creating the directory, because the umask is applied at the time of creation.
                    const UnixFileMode BindMountPermissions =
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

                    File.SetUnixFileMode(bindMountPath, BindMountPermissions);
                }

                sqlserver1.WithDataBindMount(bindMountPath);
            }

            using var app1 = builder1.Build();

            await app1.StartAsync();

            await app1.ResourceNotifications.WaitForResourceHealthyAsync(masterdb1.Resource.Name, cts.Token);

            try
            {
                var hb1 = Host.CreateApplicationBuilder();

                hb1.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{masterdb1.Resource.Name}"] = await masterdb1.Resource.ConnectionStringExpression.GetValueAsync(default),
                });

                hb1.AddSqlServerClient(masterdb1.Resource.Name);

                using var host1 = hb1.Build();

                await host1.StartAsync();

                await pipeline.ExecuteAsync(async token =>
                {
                    var conn = host1.Services.GetRequiredService<SqlConnection>();

                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        await conn.OpenAsync(token);
                    }

                    var command = conn.CreateCommand();
                    command.CommandText = """
                        DROP TABLE IF EXISTS [Cars];
                        CREATE TABLE [Cars] ([Brand] nvarchar(max) NOT NULL);
                        INSERT INTO [Cars] ([Brand]) VALUES ('BatMobile');
                        SELECT * FROM [Cars];
                    """;

                    var results = await command.ExecuteReaderAsync(token);

                    Assert.True(results.HasRows);
                }, cts.Token);

                await app1.StopAsync();

                await pipeline.ExecuteAsync(async token =>
                {
                    var conn = host1.Services.GetRequiredService<SqlConnection>();

                    try
                    {
                        await conn.OpenAsync(token);
                    }
                    catch
                    {
                        // Failing means the database is correctly down
                        return;
                    }

                    Assert.Fail("Waiting for database to be down");
                }, cts.Token);
            }
            finally
            {
                // Stops the container, or the Volume/mount would still be in use
                await app1.StopAsync();
            }

            using var builder2 = TestDistributedApplicationBuilder.Create(o => { }, testOutputHelper);
            var passwordParameter2 = builder2.AddParameter("pwd", password);

            var sqlserver2 = builder2.AddSqlServer("sqlserver2", passwordParameter2);
            var masterdb2 = sqlserver2.AddDatabase("master");

            if (useVolume)
            {
                sqlserver2.WithDataVolume(volumeName);
            }
            else
            {
                sqlserver2.WithDataBindMount(bindMountPath!);
            }

            using (var app2 = builder2.Build())
            {
                await app2.StartAsync();

                await app2.ResourceNotifications.WaitForResourceHealthyAsync(masterdb2.Resource.Name, cts.Token);

                try
                {
                    var hb2 = Host.CreateApplicationBuilder();

                    hb2.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{masterdb2.Resource.Name}"] = await masterdb2.Resource.ConnectionStringExpression.GetValueAsync(default),
                    });

                    hb2.AddSqlServerClient(masterdb2.Resource.Name);

                    using (var host2 = hb2.Build())
                    {
                        await host2.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var conn = host2.Services.GetRequiredService<SqlConnection>();

                            if (conn.State != System.Data.ConnectionState.Open)
                            {
                                await conn.OpenAsync(token);
                            }
                        }, cts.Token);

                        var conn = host2.Services.GetRequiredService<SqlConnection>();
                        var command = conn.CreateCommand();
                        command.CommandText = $"SELECT * FROM [Cars];";
                        var results = await command.ExecuteReaderAsync(cts.Token);

                        Assert.True(await results.ReadAsync(cts.Token));
                        Assert.Equal("BatMobile", results.GetString(0));
                        Assert.False(await results.ReadAsync(cts.Token));
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app2.StopAsync();
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
}
