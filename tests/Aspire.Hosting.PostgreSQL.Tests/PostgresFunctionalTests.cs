// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data;
using System.Net;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Postgres;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.PostgreSQL.Tests;

public class PostgresFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnPostgresServerBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // We use the following check added to the Postgres resource to block
        // dependent reosurces from starting. This means we'll have two checks
        // associated with the postgres resource ... the built in one and the
        // one that we add here. We'll manipulate the TCS to allow us to check
        // states at various stages of the execution.
        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var postgres = builder.AddPostgres("postgres")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddPostgres("dependentresource")
                                       .WaitFor(postgres); // Just using another postgres instance as a dependent resource.

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // What for the postgres server to start.
        await rns.WaitForResourceAsync(postgres.Resource.Name, KnownResourceStates.Running, cts.Token);

        // Wait for the dependent resource to be in the Waiting state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        // Now unblock the health check.
        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        // ... and wait for the resource as a whole to move into the health state.
        await rns.WaitForResourceAsync(postgres.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        // ... then the dependent resource should be able to move into a running state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart; // Startup should now complete.

        // ... but we'll shut everything down immediately because we are done.
        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnPostgresDatabaseBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        // We use the following check added to the Postgres resource to block
        // dependent reosurces from starting. This means we'll have two checks
        // associated with the postgres resource ... the built in one and the
        // one that we add here. We'll manipulate the TCS to allow us to check
        // states at various stages of the execution.
        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var postgres = builder.AddPostgres("postgres")
                              .WithHealthCheck("blocking_check");

        var db = postgres.AddDatabase("db");

        var dependentResource = builder.AddPostgres("dependentresource")
                                       .WaitFor(db); // Wait on the database instead of the server!

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        // What for the postgres server to start.
        await rns.WaitForResourceAsync(postgres.Resource.Name, KnownResourceStates.Running, cts.Token);

        // The database should adopt the state of the parent resource.
        await rns.WaitForResourceAsync(db.Resource.Name, KnownResourceStates.Running, cts.Token);

        // Wait for the dependent resource to be in the Waiting state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        // Now unblock the health check.
        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        // ... and wait for the resource as a whole to move into the health state.
        await rns.WaitForResourceAsync(postgres.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        // Create the database.
        var connectionString = await postgres.Resource.GetConnectionStringAsync(cts.Token);
        using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cts.Token);

        var command = connection.CreateCommand();
        command.CommandText = "CREATE DATABASE db;";
        await command.ExecuteNonQueryAsync(cts.Token);

        // ... then wait for the database to turn healthy.
        await rns.WaitForResourceAsync(db.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        // ... then the dependent resource should be able to move into a running state.
        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart; // Startup should now complete.

        // ... but we'll shut everything down immediately because we are done.
        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyPgAdminResource()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        IResourceBuilder<PgAdminContainerResource>? adminBuilder = null;
        var redis = builder.AddPostgres("postgres").WithPgAdmin(c => adminBuilder = c);
        Assert.NotNull(adminBuilder);

        using var app = builder.Build();

        await app.StartAsync();

        await app.WaitForTextAsync("Listening at", resourceName: adminBuilder.Resource.Name);

        var client = app.CreateHttpClient(adminBuilder.Resource.Name, "http");

        var path = $"/";
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyPostgresResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var postgresDbName = "db1";

        var postgres = builder.AddPostgres("pg").WithEnvironment("POSTGRES_DB", postgresDbName);
        var db = postgres.AddDatabase(postgresDbName);

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

        hb.AddNpgsqlDataSource(db.Resource.Name);

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
            await connection.OpenAsync(token);

            var command = connection.CreateCommand();
            command.CommandText = $"SELECT 1";
            var results = await command.ExecuteReaderAsync(token);

            Assert.True(results.HasRows);
        }, cts.Token);
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithPgWeb()
    {
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        IResourceBuilder<PgWebContainerResource>? pgWebBuilder = null;
        var dbName = "postgres";
        var pg = builder.AddPostgres("pg1").WithPgWeb(c => pgWebBuilder = c).AddDatabase(dbName);
        Assert.NotNull(pgWebBuilder);

        using var app = builder.Build();

        await app.StartAsync();

        await app.WaitForTextAsync("Starting server...", resourceName: pgWebBuilder.Resource.Name);

        var client = app.CreateHttpClient(pgWebBuilder.Resource.Name, "http");

        var httpContent = new MultipartFormDataContent
        {
            { new StringContent(dbName), "bookmark_id" }
        };

        client.DefaultRequestHeaders.Add("x-session-id", Guid.NewGuid().ToString());

        var response = await client.PostAsync("/api/connect", httpContent);
        response.EnsureSuccessStatusCode();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var postgresDbName = "tempdb";

        string? volumeName = null;
        string? bindMountPath = null;

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .Build();

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

            var username = "postgres";
            var password = "p@ssw0rd1";

            var usernameParameter = builder1.AddParameter("user");
            var passwordParameter = builder1.AddParameter("pwd");
            builder1.Configuration["Parameters:user"] = username;
            builder1.Configuration["Parameters:pwd"] = password;
            var postgres1 = builder1.AddPostgres("pg", usernameParameter, passwordParameter).WithEnvironment("POSTGRES_DB", postgresDbName);

            var db1 = postgres1.AddDatabase(postgresDbName);

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(postgres1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                postgres1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                postgres1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db1.Resource.Name}"] = await db1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddNpgsqlDataSource(db1.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
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
                    await app.StopAsync();
                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            usernameParameter = builder2.AddParameter("user");
            passwordParameter = builder2.AddParameter("pwd");
            builder2.Configuration["Parameters:user"] = username;
            builder2.Configuration["Parameters:pwd"] = password;

            var postgres2 = builder2.AddPostgres("pg", usernameParameter, passwordParameter);
            var db2 = postgres2.AddDatabase(postgresDbName);

            if (useVolume)
            {
                postgres2.WithDataVolume(volumeName);
            }
            else
            {
                postgres2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{db2.Resource.Name}"] = await db2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

                    hb.AddNpgsqlDataSource(db2.Resource.Name);

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
                            await connection.OpenAsync(token);

                            var command = connection.CreateCommand();
                            command.CommandText = $"SELECT * FROM cars;";
                            var results = await command.ExecuteReaderAsync(token);

                            Assert.True(await results.ReadAsync(token));
                            Assert.Equal("BatMobile", results.GetString("brand"));
                            Assert.False(await results.ReadAsync(token));
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

    [Fact]
    [RequiresDocker]
    public async Task VerifyWithInitBindMount()
    {
        // Creates a script that should be executed when the container is initialized.

        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(2) })
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

            var postgresDbName = "db1";

            var postgres = builder.AddPostgres("pg").WithEnvironment("POSTGRES_DB", postgresDbName);
            var db = postgres.AddDatabase(postgresDbName);

            postgres.WithInitBindMount(bindMountPath);

            using var app = builder.Build();

            await app.StartAsync();

            var hb = Host.CreateApplicationBuilder();

            hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{db.Resource.Name}"] = await db.Resource.ConnectionStringExpression.GetValueAsync(default)
            });

            hb.AddNpgsqlDataSource(db.Resource.Name);

            using var host = hb.Build();

            await host.StartAsync();

            // Wait until the database is available
            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
                await connection.OpenAsync(token);
                Assert.Equal(ConnectionState.Open, connection.State);
            }, cts.Token);

            await pipeline.ExecuteAsync(async token =>
            {
                using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
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
                Directory.Delete(bindMountPath, true);
            }
            catch
            {
                // Don't fail test if we can't clean the temporary folder
            }
        }
    }
}
