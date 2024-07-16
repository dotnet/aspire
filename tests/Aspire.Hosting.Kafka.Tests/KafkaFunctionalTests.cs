// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Kafka.Tests;

public class KafkaFunctionalTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task VerifyKafkaResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        var builder = CreateDistributedApplicationBuilder();

        var kafka = builder.AddKafka("kafka");

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{kafka.Resource.Name}"] = await kafka.Resource.ConnectionStringExpression.GetValueAsync(default)
        });

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
    [ActiveIssue("https://github.com/dotnet/aspire/issues/4909")]
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
            var builder1 = CreateDistributedApplicationBuilder();
            var kafka1 = builder1.AddKafka("kafka");

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(kafka1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), try to delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName);
                kafka1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                kafka1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{kafka1.Resource.Name}"] = await kafka1.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

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
                }
            }

            var builder2 = CreateDistributedApplicationBuilder();
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
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        [$"ConnectionStrings:{kafka2.Resource.Name}"] = await kafka2.Resource.ConnectionStringExpression.GetValueAsync(default)
                    });

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
                    File.Delete(bindMountPath);
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
