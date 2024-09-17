// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Kafka.Tests;

public class KafkaFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnKafkaBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddKafka("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddKafka("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceAsync(resource.Resource.Name, (re => re.Snapshot.HealthStatus == HealthStatus.Healthy), cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyKafkaResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var kafka = builder.AddKafka("kafka");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{kafka.Resource.Name}"] = await kafka.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddKafkaProducer<string, string>("kafka");
        hb.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
        {
            consumerBuilder.Config.GroupId = "aspire-consumer-group";
            consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
        });

        using var host = hb.Build();

        await host.StartAsync();

        var topic = "test-topic";

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
            .Build();
        await pipeline.ExecuteAsync(async token =>
        {
            var producer = host.Services.GetRequiredService<IProducer<string, string>>();
            for (var i = 0; i < 10; i++)
            {
                await producer.ProduceAsync(topic, new Message<string, string> { Key = "test-key", Value = $"test-value{i}" }, token);
            }
        }, cts.Token);

        var consumer = host.Services.GetRequiredService<IConsumer<string, string>>();
        consumer.Subscribe(topic);
        for (var i = 0; i < 10; i++)
        {
            var result = consumer.Consume(cts.Token);

            Assert.Equal($"test-key", result.Message.Key);
            Assert.Equal($"test-value{i}", result.Message.Value);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var topic = "test-topic";
        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var kafka1 = builder1.AddKafka("kafka");

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(kafka1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                kafka1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                Directory.CreateDirectory(bindMountPath);

                if (!OperatingSystem.IsWindows())
                {
                    // the docker container runs as a non-root user, so we need to grant other user's read/write permission
                    // to the bind mount directory.
                    // Note that we need to do this after creating the directory, because the umask is applied at the time of creation.
                    const UnixFileMode BindMountPermissions =
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                        UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;

                    File.SetUnixFileMode(bindMountPath, BindMountPermissions);
                }
                kafka1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();

                await app.WaitForTextAsync("Server started, listening for requests...", kafka1.Resource.Name);
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{kafka1.Resource.Name}"] = await kafka1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddKafkaProducer<string, string>("kafka");

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        var pipeline = new ResiliencePipelineBuilder()
                            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ProduceException<string, string>>() })
                            .Build();
                        await pipeline.ExecuteAsync(async token =>
                        {
                            var producer = host.Services.GetRequiredService<IProducer<string, string>>();
                            for (var i = 0; i < 10; i++)
                            {
                                await producer.ProduceAsync(topic, new Message<string, string> { Key = "test-key", Value = $"test-value{i}" }, token);
                            }
                        }, cts.Token);
                    }
                }
                finally
                {
                    // Stops the container, or the Volume/mount would still be in use
                    await app.StopAsync();
                    //kafka shutdown has delay,so without delay to running instance using same data and second instance failed to start.
                    await Task.Delay(TimeSpan.FromMinutes(1));

                }
            }

            using var builder2 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var kafka2 = builder2.AddKafka("kafka");

            if (useVolume)
            {
                kafka2.WithDataVolume(volumeName);
            }
            else
            {
                kafka2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                await app.WaitForTextAsync("Server started, listening for requests...", kafka1.Resource.Name);

                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{kafka2.Resource.Name}"] = await kafka2.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
                    {
                        consumerBuilder.Config.GroupId = "aspire-consumer-group";
                        consumerBuilder.Config.AutoOffsetReset = AutoOffsetReset.Earliest;
                    });

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        var pipeline = new ResiliencePipelineBuilder()
                            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<ConsumeException>() })
                            .Build();
                        pipeline.Execute(() =>
                        {
                            var consumer = host.Services.GetRequiredService<IConsumer<string, string>>();
                            consumer.Subscribe(topic);
                            for (var i = 0; i < 10; i++)
                            {
                                var result = consumer.Consume(cts.Token);

                                Assert.Equal($"test-key", result.Message.Key);
                                Assert.Equal($"test-value{i}", result.Message.Value);
                            }
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
}
