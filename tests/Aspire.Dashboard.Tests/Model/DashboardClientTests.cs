// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Dashboard.Utils;
using Google.Protobuf.WellKnownTypes;
using Aspire.DashboardService.Proto.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Microsoft.AspNetCore.InternalTesting;
using Grpc.Core;

namespace Aspire.Dashboard.Tests.Model;

public sealed class DashboardClientTests
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<DashboardOptions> _dashboardOptions;

    public DashboardClientTests()
    {
        _configuration = new ConfigurationManager();

        var options = new DashboardOptions
        {
            ResourceServiceClient =
            {
                AuthMode = ResourceClientAuthMode.Unsecured,
                Url = "http://localhost:12345"
            }
        };
        options.ResourceServiceClient.TryParseOptions(out _);

        _dashboardOptions = Options.Create(options);
    }

    [Fact]
    public async Task SubscribeResources_OnCancel_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        var (_, subscription) = await client.SubscribeResourcesAsync(CancellationToken.None).DefaultTimeout();

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token))
            {
            }
        });

        cts.Cancel();

        await TaskHelpers.WaitIgnoreCancelAsync(readTask).DefaultTimeout();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);
    }

    [Fact]
    public async Task SubscribeResources_OnDispose_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        var (_, subscription) = await client.SubscribeResourcesAsync(CancellationToken.None).DefaultTimeout();

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription)
            {
            }
        });

        await instance.DisposeAsync().DefaultTimeout();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        await TaskHelpers.WaitIgnoreCancelAsync(readTask).DefaultTimeout();
    }

    [Fact]
    public async Task SubscribeResources_ThrowsIfDisposed()
    {
        await using IDashboardClient client = CreateResourceServiceClient();

        await client.DisposeAsync().DefaultTimeout();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => client.SubscribeResourcesAsync(CancellationToken.None)).DefaultTimeout();
    }

    [Fact]
    public async Task SubscribeResources_IncreasesSubscriberCount()
    {
        await using var instance = CreateResourceServiceClient();
        instance.SetInitialDataReceived();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        _ = await client.SubscribeResourcesAsync(CancellationToken.None).DefaultTimeout();

        Assert.Equal(1, instance.OutgoingResourceSubscriberCount);

        await instance.DisposeAsync().DefaultTimeout();

        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);
    }

    [Fact]
    public async Task SubscribeResources_HasInitialData_InitialDataReturned()
    {
        await using var instance = CreateResourceServiceClient();

        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();

        var subscribeTask = client.SubscribeResourcesAsync(CancellationToken.None);

        Assert.False(subscribeTask.IsCompleted);
        Assert.Equal(0, instance.OutgoingResourceSubscriberCount);

        instance.SetInitialDataReceived([new Resource
        {
            Name = "test",
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
        }]);

        var (initialData, subscription) = await subscribeTask.DefaultTimeout();

        Assert.Single(initialData);
    }

    [Fact]
    public async Task SubscribeInteractions_OnCancel_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();

        IDashboardClient client = instance;

        var cts = new CancellationTokenSource();

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);

        var subscription = client.SubscribeInteractionsAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingInteractionSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription.WithCancellation(cts.Token))
            {
            }
        });

        cts.Cancel();

        await TaskHelpers.WaitIgnoreCancelAsync(readTask).DefaultTimeout();

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);
    }

    [Fact]
    public async Task SubscribeInteractions_OnDispose_ChannelRemoved()
    {
        await using var instance = CreateResourceServiceClient();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);

        var subscription = client.SubscribeInteractionsAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingInteractionSubscriberCount);

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in subscription)
            {
            }
        });

        await instance.DisposeAsync().DefaultTimeout();

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);

        await TaskHelpers.WaitIgnoreCancelAsync(readTask).DefaultTimeout();
    }

    [Fact]
    public async Task SubscribeInteractions_ThrowsIfDisposed()
    {
        await using IDashboardClient client = CreateResourceServiceClient();

        await client.DisposeAsync().DefaultTimeout();

        Assert.Throws<ObjectDisposedException>(() => client.SubscribeInteractionsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SubscribeInteractions_IncreasesSubscriberCount()
    {
        await using var instance = CreateResourceServiceClient();

        IDashboardClient client = instance;

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);

        _ = client.SubscribeInteractionsAsync(CancellationToken.None);

        Assert.Equal(1, instance.OutgoingInteractionSubscriberCount);

        await instance.DisposeAsync().DefaultTimeout();

        Assert.Equal(0, instance.OutgoingInteractionSubscriberCount);
    }

    [Fact]
    public async Task WhenConnected_InteractionMethodUnimplemented_InteractionWatchCompleted()
    {
        await using var instance = CreateResourceServiceClient();
        instance.SetDashboardServiceClient(new MockDashboardServiceClient());

        await instance.WhenConnected.DefaultTimeout();

        await instance.InteractionWatchCompleteTask.DefaultTimeout();
    }

    private sealed class MockDashboardServiceClient : Aspire.DashboardService.Proto.V1.DashboardService.DashboardServiceClient
    {
        public override AsyncDuplexStreamingCall<WatchInteractionsRequestUpdate, WatchInteractionsResponseUpdate> WatchInteractions(CallOptions options)
        {
            return new AsyncDuplexStreamingCall<WatchInteractionsRequestUpdate, WatchInteractionsResponseUpdate>(
                new ClientStreamWriter<WatchInteractionsRequestUpdate>(),
                new AsyncStreamReader<WatchInteractionsResponseUpdate>(),
                Task.FromResult(new Metadata()),
                () => new Status(StatusCode.Unimplemented, "Unimplemented!"),
                () => new Metadata(),
                () => { });
        }

        public override AsyncUnaryCall<ApplicationInformationResponse> GetApplicationInformationAsync(ApplicationInformationRequest request, CallOptions options)
        {
            return new AsyncUnaryCall<ApplicationInformationResponse>(
                Task.FromResult(new ApplicationInformationResponse
                {
                    ApplicationName = "TestApplication"
                }),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }

        public override AsyncServerStreamingCall<WatchResourcesUpdate> WatchResources(WatchResourcesRequest request, CallOptions options)
        {
            return new AsyncServerStreamingCall<WatchResourcesUpdate>(
                new AsyncStreamReader<WatchResourcesUpdate>(),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }

    private sealed class AsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        public T Current { get; } = default!;

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class ClientStreamWriter<T> : IClientStreamWriter<T>
    {
        public WriteOptions? WriteOptions { get; set; }

        public Task CompleteAsync()
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(T message)
        {
            throw new NotImplementedException();
        }
    }

    private DashboardClient CreateResourceServiceClient()
    {
        return new DashboardClient(NullLoggerFactory.Instance, _configuration, _dashboardOptions, new MockKnownPropertyLookup());
    }
}
