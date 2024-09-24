// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Utils;
using Xunit;
using Xunit.Abstractions;
using Polly;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace Aspire.Hosting.Nats.Tests;

public class NatsFunctionalTests(ITestOutputHelper testOutputHelper)
{
    private const string StreamName = "test-stream";
    private const string SubjectName = "test-subject";

    [Fact]
    [RequiresDocker]
    public async Task VerifyNatsResource()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var pipeline = new ResiliencePipelineBuilder()
                            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NatsException>() })
                            .Build();

        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var nats = builder.AddNats("nats")
            .WithJetStream();

        using var app = builder.Build();

        await app.StartAsync();

        var hb = Host.CreateApplicationBuilder();

        hb.Configuration[$"ConnectionStrings:{nats.Resource.Name}"] = await nats.Resource.ConnectionStringExpression.GetValueAsync(default);

        hb.AddNatsClient("nats", configureOptions: opts =>
        {
            var jsonRegistry = new NatsJsonContextSerializerRegistry(AppJsonContext.Default);
            return opts with { SerializerRegistry = jsonRegistry };
        });

        hb.AddNatsJetStream();

        using var host = hb.Build();

        await host.StartAsync();

        await pipeline.ExecuteAsync(async token =>
        {
            var jetStream = host.Services.GetRequiredService<INatsJSContext>();

            await CreateTestData(jetStream, token);
            await ConsumeTestData(jetStream, token);
        }, cts.Token);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequiresDocker]
    public async Task WithDataShouldPersistStateBetweenUsages(bool useVolume)
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var pipeline = new ResiliencePipelineBuilder()
                            .AddRetry(new() { MaxRetryAttempts = 10, Delay = TimeSpan.FromSeconds(1), ShouldHandle = new PredicateBuilder().Handle<NatsException>() })
                            .Build();
        string? volumeName = null;
        string? bindMountPath = null;

        try
        {
            using var builder1 = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);
            var nats1 = builder1.AddNats("nats")
                .WithJetStream();

            if (useVolume)
            {
                // Use a deterministic volume name to prevent them from exhausting the machines if deletion fails
                volumeName = VolumeNameGenerator.CreateVolumeName(nats1, nameof(WithDataShouldPersistStateBetweenUsages));

                // if the volume already exists (because of a crashing previous run), delete it
                DockerUtils.AttemptDeleteDockerVolume(volumeName, throwOnFailure: true);
                nats1.WithDataVolume(volumeName);
            }
            else
            {
                bindMountPath = Directory.CreateTempSubdirectory().FullName;
                nats1.WithDataBindMount(bindMountPath);
            }

            using (var app = builder1.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{nats1.Resource.Name}"] = await nats1.Resource.ConnectionStringExpression.GetValueAsync(default);

                    hb.AddNatsClient("nats", configureOptions: opts =>
                    {
                        var jsonRegistry = new NatsJsonContextSerializerRegistry(AppJsonContext.Default);
                        return opts with { SerializerRegistry = jsonRegistry };
                    });

                    hb.AddNatsJetStream();

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var jetStream = host.Services.GetRequiredService<INatsJSContext>();
                            await CreateTestData(jetStream, token);
                            await ConsumeTestData(jetStream, token);

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
            var nats2 = builder2.AddNats("nats")
                .WithJetStream();

            if (useVolume)
            {
                nats2.WithDataVolume(volumeName);
            }
            else
            {
                nats2.WithDataBindMount(bindMountPath!);
            }

            using (var app = builder2.Build())
            {
                await app.StartAsync();
                try
                {
                    var hb = Host.CreateApplicationBuilder();

                    hb.Configuration[$"ConnectionStrings:{nats2.Resource.Name}"] = await nats2.Resource.ConnectionStringExpression.GetValueAsync(default);
                    hb.AddNatsClient("nats", configureOptions: opts =>
                    {
                        var jsonRegistry = new NatsJsonContextSerializerRegistry(AppJsonContext.Default);
                        return opts with { SerializerRegistry = jsonRegistry };
                    });

                    hb.AddNatsJetStream();

                    using (var host = hb.Build())
                    {
                        await host.StartAsync();

                        await pipeline.ExecuteAsync(async token =>
                        {
                            var jetStream = host.Services.GetRequiredService<INatsJSContext>();
                            await ConsumeTestData(jetStream, token);
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

    private static async Task ConsumeTestData(INatsJSContext jetStream, CancellationToken token)
    {
        var stream = await jetStream.GetStreamAsync(StreamName, cancellationToken: token);
        var consumer = await stream.CreateOrderedConsumerAsync(cancellationToken: token);

        var events = new List<AppEvent>();
        await foreach (var msg in consumer.ConsumeAsync<AppEvent>(cancellationToken: token))
        {
            events.Add(msg.Data!);
            await msg.AckAsync(cancellationToken: token);
            if (msg.Metadata?.NumPending == 0)
            {
                break;
            }
        }

        for (var i = 0; i < 10; i++)
        {
            var @event = events[i];
            Assert.Equal($"test-event-{i}", @event.Name);
            Assert.Equal($"test-event-description-{i}", @event.Description);
        }
    }

    private static async Task CreateTestData(INatsJSContext jetStream, CancellationToken token)
    {
        var stream = await jetStream.CreateStreamAsync(new StreamConfig(StreamName, [SubjectName]), cancellationToken: token);
        Assert.Equal(StreamName, stream.Info.Config.Name);

        for (var i = 0; i < 10; i++)
        {
            var appEvent = new AppEvent(SubjectName, $"test-event-{i}", $"test-event-description-{i}", i);
            var ack = await jetStream.PublishAsync(appEvent.Subject, appEvent, cancellationToken: token);
            ack.EnsureSuccess();
        }
    }

    [Fact]
    [RequiresDocker]
    public async Task VerifyWaitForOnNatsBlocksDependentResources()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var builder = TestDistributedApplicationBuilder.CreateWithTestContainerRegistry(testOutputHelper);

        var healthCheckTcs = new TaskCompletionSource<HealthCheckResult>();
        builder.Services.AddHealthChecks().AddAsyncCheck("blocking_check", () =>
        {
            return healthCheckTcs.Task;
        });

        var resource = builder.AddNats("resource")
                              .WithHealthCheck("blocking_check");

        var dependentResource = builder.AddNats("dependentresource")
                                       .WaitFor(resource);

        using var app = builder.Build();

        var pendingStart = app.StartAsync(cts.Token);

        var rns = app.Services.GetRequiredService<ResourceNotificationService>();

        await rns.WaitForResourceAsync(resource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Waiting, cts.Token);

        healthCheckTcs.SetResult(HealthCheckResult.Healthy());

        await rns.WaitForResourceHealthyAsync(resource.Resource.Name, cts.Token);

        await rns.WaitForResourceAsync(dependentResource.Resource.Name, KnownResourceStates.Running, cts.Token);

        await pendingStart;

        await app.StopAsync();
    }
}
