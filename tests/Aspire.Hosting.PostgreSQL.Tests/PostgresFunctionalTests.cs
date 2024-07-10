// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;
using Xunit;

namespace Aspire.Hosting.PostgreSQL.Tests;

public class PostgresFunctionalTests
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyPostgresResource()
    {
        var builder = TestDistributedApplicationBuilder.Create();
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

    [Fact]
    [RequiresDocker]
    public async Task WithDataVolumeShouldPersistStateBetweenUsages()
    {
        var postgresDbName = "tempdb";

        var builder1 = TestDistributedApplicationBuilder.Create();

        var username = "postgres";
        var password = PasswordGenerator.Generate(6, true, true, true, true, 1, 1, 1, 1);
        var usernameParameter = builder1.AddParameter("user");
        var passwordParameter = builder1.AddParameter("pwd");
        builder1.Configuration["Parameters:user"] = username;
        builder1.Configuration["Parameters:pwd"] = password;
        var postgres1 = builder1.AddPostgres("pg", usernameParameter, passwordParameter).WithEnvironment("POSTGRES_DB", postgresDbName);
        var db1 = postgres1.AddDatabase(postgresDbName);

        // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
        var volumeName = VolumeNameGenerator.CreateVolumeName(postgres1, nameof(WithDataVolumeShouldPersistStateBetweenUsages));
        DockerUtils.AttemptDeleteDockerVolume(volumeName);

        postgres1.WithDataVolume(volumeName);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        using (var app = builder1.Build())
        {
            await app.StartAsync();

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

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        var builder2 = TestDistributedApplicationBuilder.Create();
        usernameParameter = builder2.AddParameter("user");
        passwordParameter = builder2.AddParameter("pwd");
        builder2.Configuration["Parameters:user"] = username;
        builder2.Configuration["Parameters:pwd"] = password;

        var postgres2 = builder2.AddPostgres("pg", usernameParameter, passwordParameter);
        var db2 = postgres2.AddDatabase(postgresDbName);
        postgres2.WithDataVolume(volumeName);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

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

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        DockerUtils.AttemptDeleteDockerVolume(volumeName);
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDataBindMountShouldPersistStateBetweenUsages()
    {
        var bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        if (Directory.Exists(bindMountPath))
        {
            Directory.Delete(bindMountPath, true);
        }

        Directory.CreateDirectory(bindMountPath);

        var postgresDbName = "tempdb";

        var builder1 = TestDistributedApplicationBuilder.Create();

        var username = "postgres";
        var password = PasswordGenerator.Generate(6, true, true, true, true, 1, 1, 1, 1);
        var usernameParameter = builder1.AddParameter("user");
        var passwordParameter = builder1.AddParameter("pwd");
        builder1.Configuration["Parameters:user"] = username;
        builder1.Configuration["Parameters:pwd"] = password;
        var postgres1 = builder1.AddPostgres("pg", usernameParameter, passwordParameter).WithEnvironment("POSTGRES_DB", postgresDbName);
        var db1 = postgres1.AddDatabase(postgresDbName);

        postgres1.WithDataBindMount(bindMountPath);

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>() })
            .AddTimeout(TimeSpan.FromSeconds(5))
            .Build();

        using (var app = builder1.Build())
        {
            await app.StartAsync();

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

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        var builder2 = TestDistributedApplicationBuilder.Create();
        usernameParameter = builder2.AddParameter("user");
        passwordParameter = builder2.AddParameter("pwd");
        builder2.Configuration["Parameters:user"] = username;
        builder2.Configuration["Parameters:pwd"] = password;

        var postgres2 = builder2.AddPostgres("pg", usernameParameter, passwordParameter);
        var db2 = postgres2.AddDatabase(postgresDbName);
        postgres2.WithDataBindMount(bindMountPath);

        using (var app = builder2.Build())
        {
            await app.StartAsync();

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

            // Stops the container, or the Volume would still be in use
            await app.StopAsync();
        }

        try
        {
            Directory.Delete(bindMountPath);
        }
        catch
        {
            // Don't fail test if we can't clean the temporary folder
        }
    }

        //[Fact]
        //[RequiresDocker]
        //public async Task PersistenceIsDisabledByDefault()
        //{
        //    // Checks that without enabling Redis Persistence the tests fail

        //    var builder1 = TestDistributedApplicationBuilder.Create();
        //    var redis1 = builder1.AddRedis("redis");

        //    using (var app = builder1.Build())
        //    {
        //        await app.StartAsync();

        //        var hb = Host.CreateApplicationBuilder();

        //        // BGSAVE is only available in admin mode, enable it for this instance
        //        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        //        {
        //            [$"ConnectionStrings:{redis1.Resource.Name}"] = $"{await redis1.Resource.GetConnectionStringAsync()},allowAdmin=true"
        //        });

        //        hb.AddRedisClient(redis1.Resource.Name);

        //        using (var host = hb.Build())
        //        {
        //            await host.StartAsync();

        //            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        //            var db = redisClient.GetDatabase();

        //            await db.StringSetAsync("key", "value");
        //        }

        //        await app.StopAsync();
        //    }

        //    var builder2 = TestDistributedApplicationBuilder.Create();
        //    var redis2 = builder2.AddRedis("redis");

        //    using (var app = builder2.Build())
        //    {
        //        await app.StartAsync();

        //        var hb = Host.CreateApplicationBuilder();

        //        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        //        {
        //            [$"ConnectionStrings:{redis2.Resource.Name}"] = await redis2.Resource.GetConnectionStringAsync()
        //        });

        //        hb.AddRedisClient(redis2.Resource.Name);

        //        using (var host = hb.Build())
        //        {
        //            await host.StartAsync();

        //            var redisClient = host.Services.GetRequiredService<IConnectionMultiplexer>();

        //            var db = redisClient.GetDatabase();

        //            var value = await db.StringGetAsync("key");

        //            Assert.True(value.IsNull);
        //        }

        //        await app.StopAsync();
        //    }
        //}
    }
