// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable ASPIRECERTIFICATES001

namespace Aspire.Hosting.Redis.Tests;

public class AddRedisTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void AddRedisAddsHealthCheckAnnotationToResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("redis");
        Assert.Single(redis.Resource.Annotations, a => a is HealthCheckAnnotation hca && hca.Key == "redis_check");
    }

    [Fact]
    public void AddRedisContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddRedisContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis", port: 9813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());
        Assert.Equal("myRedis", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(9813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(RedisContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(RedisContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(RedisContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void RedisCreatesConnectionStringWithPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddRedis("myRedis", password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void RedisCreatesConnectionStringWithPasswordAndPort()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var password = "p@ssw0rd1";
        var pass = appBuilder.AddParameter("pass", password);
        appBuilder.AddRedis("myRedis", port: 3000, password: pass);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task RedisCreatesConnectionStringWithDefaultPassword()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddRedis("myRedis")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={myRedis-password.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [Fact]
    public async Task VerifyDefaultManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("redis");

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={redis-password.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{redis-password.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyWithoutPasswordManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("redis").WithPassword(null);

        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server"
              ],
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyWithPasswordManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var password = "p@ssw0rd1";
        builder.Configuration["Parameters:pass"] = password;

        var pass = builder.AddParameter("pass");
        var redis = builder.AddRedis("redis", password: pass);
        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={pass.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyWithPasswordValueNotProvidedManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var pass = builder.AddParameter("pass");
        var redis = builder.AddRedis("redis", password: pass);
        var manifest = await ManifestUtils.GetManifest(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{redis.bindings.tcp.host}:{redis.bindings.tcp.port},password={pass.value}",
              "image": "{{RedisContainerImageTags.Registry}}/{{RedisContainerImageTags.Image}}:{{RedisContainerImageTags.Tag}}",
              "entrypoint": "/bin/sh",
              "args": [
                "-c",
                "redis-server --requirepass $REDIS_PASSWORD"
              ],
              "env": {
                "REDIS_PASSWORD": "{pass.value}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 6379
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public void WithRedisCommanderAddsRedisCommanderResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis1").WithRedisCommander();
        builder.AddRedis("myredis2").WithRedisCommander();

        Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
    }

    [Fact]
    public void WithRedisInsightAddsWithRedisInsightResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis1").WithRedisInsight();
        builder.AddRedis("myredis2").WithRedisInsight();

        var redisinsight = builder.Resources.Single(r => r.Name.Equals("redisinsight"));

        Assert.NotNull(redisinsight);
    }

    [Fact]
    public async Task WithRedisInsightProducesCorrectEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedis("myredis1").WithRedisInsight();
        var redis2 = builder.AddRedis("myredis2").WithRedisInsight();
        var redis3 = builder.AddRedis("myredis3").WithRedisInsight().WithPassword(null);
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002));

        var redisInsight = Assert.Single(builder.Resources.OfType<RedisInsightResource>());
        var envs = await redisInsight.GetEnvironmentVariableValuesAsync();

        Assert.Collection(envs,
            (item) =>
            {
                Assert.Equal("RI_REDIS_HOST1", item.Key);
                Assert.Equal($"{redis1.Resource.Name}.dev.internal", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_PORT1", item.Key);
                Assert.Equal($"{redis1.Resource.PrimaryEndpoint.TargetPort!.Value}", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_ALIAS1", item.Key);
                Assert.Equal(redis1.Resource.Name, item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_PASSWORD1", item.Key);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal(redis1.Resource.PasswordParameter!.Value, item.Value);
#pragma warning restore CS0618 // Type or member is obsolete
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_HOST2", item.Key);
                Assert.Equal($"{redis2.Resource.Name}.dev.internal", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_PORT2", item.Key);
                Assert.Equal($"{redis2.Resource.PrimaryEndpoint.TargetPort!.Value}", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_ALIAS2", item.Key);
                Assert.Equal(redis2.Resource.Name, item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_PASSWORD2", item.Key);
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.Equal(redis2.Resource.PasswordParameter!.Value, item.Value);
#pragma warning restore CS0618 // Type or member is obsolete
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_HOST3", item.Key);
                Assert.Equal($"{redis3.Resource.Name}.dev.internal", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_PORT3", item.Key);
                Assert.Equal($"{redis3.Resource.PrimaryEndpoint.TargetPort!.Value}", item.Value);
            },
            (item) =>
            {
                Assert.Equal("RI_REDIS_ALIAS3", item.Key);
                Assert.Equal(redis3.Resource.Name, item.Value);
            });

    }

    [Fact]
    public void WithRedisCommanderSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customrediscommander", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
    }

    [Fact]
    public void WithRedisInsightSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisInsight(c =>
        {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisInsightResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customrediscommander", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisCommander(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Fact]
    public void WithRedisInsightSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis").WithRedisInsight(c =>
        {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisInsightResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Fact]
    public void VerifyRedisResourceWithHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddRedis("myredis")
            .WithHostPort(1000);

        var resource = Assert.Single(builder.Resources.OfType<RedisResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task VerifyRedisResourceWithPassword(bool withPassword)
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var redis = builder
            .AddRedis("myRedis")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        var password = "p@ssw0rd1";
        if (withPassword)
        {
            var pass = builder.AddParameter("pass", password);
            redis.WithPassword(pass);
        }
        else
        {
            redis.WithPassword(null);
        }

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        if (withPassword)
        {
            Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
            Assert.Equal($"localhost:5001,password={password}", connectionString);
        }
        else
        {
            Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
            Assert.Equal($"localhost:5001", connectionString);
        }
    }

    [Fact]
    public async Task SingleRedisInstanceWithoutPasswordProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis1").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        var commander = builder.Resources.Single(r => r.Name.Equals("rediscommander"));

        await builder.Eventing.PublishAsync<BeforeResourceStartedEvent>(new(commander, app.Services));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal($"myredis1:{redis.Resource.Name}:6379:0:{redis.Resource.PasswordParameter?.Value}", config["REDIS_HOSTS"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Fact]
    public async Task SingleRedisInstanceWithPasswordProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = "p@ssw0rd1";
        var pass = builder.AddParameter("pass", password);
        var redis = builder.AddRedis("myredis1", password: pass).WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        var commander = builder.Resources.Single(r => r.Name.Equals("rediscommander"));

        await builder.Eventing.PublishAsync<BeforeResourceStartedEvent>(new(commander, app.Services));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal($"myredis1:{redis.Resource.Name}:6379:0:{password}", config["REDIS_HOSTS"]);
    }

    [Fact]
    public async Task MultipleRedisInstanceProducesCorrectRedisHostsVariable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis1 = builder.AddRedis("myredis1").WithRedisCommander();
        var redis2 = builder.AddRedis("myredis2").WithRedisCommander();
        using var app = builder.Build();

        // Add fake allocated endpoints.
        redis1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));
        redis2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        var commander = builder.Resources.Single(r => r.Name.Equals("rediscommander"));

        await builder.Eventing.PublishAsync<BeforeResourceStartedEvent>(new(commander, app.Services));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            commander,
            DistributedApplicationOperation.Run,
            TestServiceProvider.Instance);

#pragma warning disable CS0618 // Type or member is obsolete
        Assert.Equal($"myredis1:{redis1.Resource.Name}:6379:0:{redis1.Resource.PasswordParameter?.Value},myredis2:myredis2:6379:0:{redis2.Resource.PasswordParameter?.Value}", config["REDIS_HOSTS"]);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataVolume();
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal($"{builder.GetVolumePrefix()}-myRedis-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis");
        if (isReadOnly.HasValue)
        {
            redis.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            redis.WithDataBindMount("mydata");
        }

        var volumeAnnotation = redis.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public async Task WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                              .WithDataVolume();

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 60 1", args);
    }

    [Fact]
    public async Task WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume(isReadOnly: true);

        var args = await GetCommandLineArgs(redis);
        Assert.DoesNotContain("--save", args);
    }

    [Fact]
    public async Task WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata");

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 60 1", args);
    }

    [Fact]
    public async Task WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                           .WithDataBindMount("myredisdata", isReadOnly: true);

        var args = await GetCommandLineArgs(redis);
        Assert.DoesNotContain("--save", args);
    }

    [Fact]
    public async Task WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        var args = await GetCommandLineArgs(redis);
        Assert.Contains("--save 10 2", args);

        // ensure `--save` is not added twice
        var saveIndex = args.IndexOf("--save");
        Assert.DoesNotContain("--save", args.Substring(saveIndex + 1));
    }

    private static async Task<string> GetCommandLineArgs(IResourceBuilder<RedisResource> builder)
    {
        var args = await ArgumentEvaluator.GetArgumentListAsync(builder.Resource);
        return string.Join(" ", args);
    }

    [Fact]
    public void WithPersistenceAddsCommandLineArgsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        var redis = builder.AddRedis("myRedis")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.True(redis.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.NotNull(argsAnnotations.SingleOrDefault());
    }

    [Fact]
    public async Task AddRedisContainerWithPasswordAnnotationMetadata()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var password = "p@ssw0rd1";
        var pass = builder.AddParameter("pass", password);
        var redis = builder.
            AddRedis("myRedis", password: pass)
           .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001));

        using var app = builder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<RedisResource>());

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.Equal("{myRedis.bindings.tcp.host}:{myRedis.bindings.tcp.port},password={pass.value}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith($"localhost:5001,password={password}", connectionString);
    }

    [Fact]
    public async Task RedisInsightEnvironmentCallbackIsIdempotent()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var redis = appBuilder.AddRedis("redis")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 6379))
            .WithRedisInsight();

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var redisInsightResource = Assert.Single(appModel.Resources.OfType<RedisInsightResource>());

        // Call GetEnvironmentVariableValuesAsync multiple times to ensure callbacks are idempotent
        var config1 = await redisInsightResource.GetEnvironmentVariableValuesAsync();
        var config2 = await redisInsightResource.GetEnvironmentVariableValuesAsync();

        // Both calls should succeed and return the same values
        Assert.Equal(config1.Count, config2.Count);
        Assert.Contains(config1, kvp => kvp.Key == "RI_REDIS_HOST1");
        Assert.Contains(config2, kvp => kvp.Key == "RI_REDIS_HOST1");
        Assert.Equal(
            config1.First(kvp => kvp.Key == "RI_REDIS_HOST1").Value,
            config2.First(kvp => kvp.Key == "RI_REDIS_HOST1").Value);
    }

    [Fact]
    public void WithoutCertificateKeyPairDisablesTlsConfiguration()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis").WithoutServerAuthenticationCertificate();

        var annotation = Assert.Single(redis.Resource.Annotations.OfType<ServerAuthenticationCertificateAnnotation>());
        Assert.False(annotation.UseDeveloperCertificate);
        Assert.Null(annotation.Certificate);
        Assert.Null(annotation.Password);
    }

    [Fact]
    [RequiresDevCert]
    public void WithDeveloperCertificateKeyPairEnablesDeveloperCertificate()
    {
        var builder = DistributedApplication.CreateBuilder();
        var redis = builder.AddRedis("myredis").WithServerAuthenticationDeveloperCertificate();

        var annotation = Assert.Single(redis.Resource.Annotations.OfType<ServerAuthenticationCertificateAnnotation>());
        Assert.True(annotation.UseDeveloperCertificate);
        Assert.Null(annotation.Certificate);
        Assert.Null(annotation.Password);
    }

    [Fact]
    [RequiresDevCert]
    public void WithDeveloperCertificateKeyPairWithPasswordStoresPassword()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("certpass", "test123");
        var redis = builder.AddRedis("myredis").WithServerAuthenticationDeveloperCertificate(password);

        var annotation = Assert.Single(redis.Resource.Annotations.OfType<ServerAuthenticationCertificateAnnotation>());
        Assert.True(annotation.UseDeveloperCertificate);
        Assert.Null(annotation.Certificate);
        Assert.Equal(password.Resource, annotation.Password);
    }

    [Fact]
    public void WithCertificateKeyPairUsesProvidedCertificate()
    {
        var builder = DistributedApplication.CreateBuilder();

        // Create a test certificate with private key
        using var cert = CreateTestCertificate();
        var redis = builder.AddRedis("myredis").WithServerAuthenticationCertificate(cert);

        var annotation = Assert.Single(redis.Resource.Annotations.OfType<ServerAuthenticationCertificateAnnotation>());
        Assert.Null(annotation.UseDeveloperCertificate);
        Assert.Equal(cert, annotation.Certificate);
        Assert.Null(annotation.Password);
    }

    [Fact]
    public void WithCertificateKeyPairWithPasswordStoresPassword()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("certpass", "test123");

        // Create a test certificate with private key
        using var cert = CreateTestCertificate();
        var redis = builder.AddRedis("myredis").WithServerAuthenticationCertificate(cert, password);

        var annotation = Assert.Single(redis.Resource.Annotations.OfType<ServerAuthenticationCertificateAnnotation>());
        Assert.Null(annotation.UseDeveloperCertificate);
        Assert.Equal(cert, annotation.Certificate);
        Assert.Equal(password.Resource, annotation.Password);
    }

    [Fact]
    public async Task RedisWithCertificateHasCorrectConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);
        using var cert = CreateTestCertificate();

        var redis = builder.AddRedis("myredis").WithServerAuthenticationCertificate(cert);

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Simulate the BeforeStartEvent to enable TLS
        var beforeStartEvent = new BeforeStartEvent(app.Services, appModel);
        await builder.Eventing.PublishAsync(beforeStartEvent);

        Assert.True(redis.Resource.TlsEnabled);
    }

    [Fact]
    public void RedisWithoutCertificateHasCorrectConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create(testOutputHelper);

        var redis = builder.AddRedis("myredis").WithoutServerAuthenticationCertificate();

        using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Simulate the BeforeStartEvent
        var beforeStartEvent = new BeforeStartEvent(app.Services, appModel);
        Assert.False(redis.Resource.TlsEnabled);
    }

    private static X509Certificate2 CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Add basic constraints
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));

        // Add key usage
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));

        // Create self-signed certificate
        var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        return certificate;
    }
}
