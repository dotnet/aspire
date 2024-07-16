// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.PostgreSQL.Tests;

public class PostgresFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyPostgresResource()
    {
        var builder = CreateDistributedApplicationBuilder();

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

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        await pipeline.ExecuteAsync(
            async token =>
            {
                using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
                await connection.OpenAsync(token);

                var command = connection.CreateCommand();
                command.CommandText = $"SELECT 1";
                var results = await command.ExecuteReaderAsync(token);

                Assert.True(results.HasRows);
            });
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

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        try
        {
            var builder1 = CreateDistributedApplicationBuilder();

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

                // If the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                postgres1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
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

                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
                                await connection.OpenAsync(token);

                                var command = connection.CreateCommand();
                                command.CommandText = $"CREATE TABLE cars (brand VARCHAR(255)); INSERT INTO cars (brand) VALUES ('BatMobile'); SELECT * FROM cars;";
                                var results = await command.ExecuteReaderAsync(token);

                                Assert.True(results.HasRows);
                            });
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                }
            }

            var builder2 = CreateDistributedApplicationBuilder();
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

                        await pipeline.ExecuteAsync(
                            async token =>
                            {
                                using var connection = host.Services.GetRequiredService<NpgsqlConnection>();
                                await connection.OpenAsync(token);

                                var command = connection.CreateCommand();
                                command.CommandText = $"SELECT * FROM cars;";
                                var results = await command.ExecuteReaderAsync(token);

                                Assert.True(results.HasRows);
                            });
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

    private TestDistributedApplicationBuilder CreateDistributedApplicationBuilder()
    {
        var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry();
        builder.Services.AddXunitLogging(testOutputHelper);
        return builder;
    }
}
