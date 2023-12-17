// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Dashboard.Utils;
using Aspire.V1;
using Grpc.Core;
using Grpc.Net.Client;

namespace Aspire.Dashboard.Model;

///// <summary>
///// Provides data about active resources to external components, such as the dashboard.
///// </summary>
//public interface IResourceService
//{
//    string ApplicationName { get; }
//
//    /// <summary>
//    /// Gets the current set of resources and a stream of updates.
//    /// </summary>
//    /// <remarks>
//    /// The returned subscription will not complete on its own.
//    /// Callers are required to manage the lifetime of the subscription,
//    /// using cancellation during enumeration.
//    /// </remarks>
//    ResourceSubscription SubscribeResources();
//
//    /// <summary>
//    /// Gets a stream of console log messages for the specified resource.
//    /// Includes messages logged both before and after this method call.
//    /// </summary>
//    /// <remarks>
//    /// <para>The returned sequence may end when the resource terminates.
//    /// It is up to the implementation.</para>
//    /// </remarks>
//    /// <para>It is important that callers trigger <paramref name="cancellationToken"/>
//    /// so that resources owned by the sequence and its consumers can be freed.</para>
//    IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken);
//}

public sealed record ResourceSubscription(
    ImmutableArray<ResourceViewModel> InitialState,
    IAsyncEnumerable<ResourceChange> Subscription);

internal sealed class DashboardClient
{
    async Task WatchResourcesAsync(string address, CancellationToken cancellationToken)
    {
        var resourceById = new Dictionary<ResourceId, Resource>();

        var (channel, client) = CreateChannel(address);

        var errorCount = 0;

        try
        {
            await channel.ConnectAsync(cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                if (channel.State == ConnectivityState.Shutdown)
                {
                    Console.WriteLine("Channel has shut down. Recreating connection.");

                    channel.Dispose();

                    (channel, client) = CreateChannel(address);
                }

                if (errorCount > 0)
                {
                    // The most recent attempt failed. There may be more than one failure.
                    // We wait for a period of time determined by the number of errors,
                    // where the time grows exponentially, until a threshold.
                    var delay = ExponentialBackOff(errorCount, maxSeconds: 15);

                    //
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                try
                {
                    await ProcessData().ConfigureAwait(false);

                    errorCount = 0;
                }
                catch (RpcException ex)
                {
                    errorCount++;

                    // TODO how to log messages
                    Console.WriteLine($"Error {errorCount} watching resources: {ex.Message}");
                }
            }
        }
        finally
        {
            channel.Dispose();
        }

        Console.WriteLine("Stopping resource watch");

        channel.Dispose();

        static TimeSpan ExponentialBackOff(int errorCount, double maxSeconds)
        {
            return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, errorCount - 1), maxSeconds));
        }

        async Task ProcessData()
        {
            Console.WriteLine("Starting watch");

            var call = client.WatchResources(new WatchResourcesRequest { IsReconnect = false }, cancellationToken: cancellationToken);

            await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                Console.WriteLine($"Response type: {response.KindCase}");

                // The most reliable way to check that a streaming call succeeded is to successfully read a response.
                if (errorCount > 0)
                {
                    resourceById.Clear();
                    errorCount = 0;
                }

                if (response.KindCase == WatchResourcesUpdate.KindOneofCase.InitialData)
                {
                    // Copy initial snapshot into model.
                    foreach (var resource in response.InitialData.Resources)
                    {
                        resourceById[resource.ResourceId] = resource.ToViewModel();
                    }
                }
                else if (response.KindCase == WatchResourcesUpdate.KindOneofCase.Changes)
                {
                    // Apply changes to the model.
                    foreach (var change in response.Changes.Value)
                    {
                        if (change.KindCase == WatchResourcesChange.KindOneofCase.Upsert)
                        {
                            // Upsert (i.e. add or replace)
                            resourceById[change.Upsert.ResourceId] = change.Upsert;
                        }
                        else if (change.KindCase == WatchResourcesChange.KindOneofCase.Delete)
                        {
                            // Remove
                            resourceById.Remove(change.Delete.ResourceId);
                        }
                    }
                }
                else
                {
                    throw new FormatException("Unsupported response kind: " + response.KindCase);
                }

                Console.WriteLine($"Current resource count: {resourceById.Count}");
            }
        }

        static (GrpcChannel, DashboardService.DashboardServiceClient) CreateChannel(string address)
        {
            var httpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingDelay = TimeSpan.FromSeconds(20),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                PooledConnectionIdleTimeout = TimeSpan.FromHours(2)
            };

            var channel = GrpcChannel.ForAddress(
                address,
                channelOptions: new() { HttpHandler = httpHandler });

            DashboardService.DashboardServiceClient client = new(channel);

            return (channel, client);
        }
    }
}

internal static class MessageExtensions
{
    public static ResourceViewModel ToViewModel(this Resource resource)
    {
        return new()
        {
            CreationTimeStamp = resource.CreatedAt.ToDateTime(),
            Properties = resource.Properties.ToFrozenDictionary(data => data.Name, data => data.Value, StringComparers.ResourceDataKey),
            DisplayName = resource.DisplayName,
            Endpoints = asd,
            Environment = asd,
            ExpectedEndpointsCount = asd,
            Name = resource.,
            ResourceType = asd,
            Services = asd,
            State = resource.HasState ? resource.State : null,
            Uid = resource.ResourceId.Uid,
        };
    }
}
