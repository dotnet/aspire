// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.PubSub;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dapr.PluggableComponents;

internal sealed class MemoryPubSub : IPubSub
{
    private readonly ILogger<MemoryPubSub> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, ConcurrentBag<PubSubPublishRequest>> _queues = new ConcurrentDictionary<string, ConcurrentBag<PubSubPublishRequest>>();

    public MemoryPubSub(ILogger<MemoryPubSub> logger)
    {
        this._logger = logger;
    }

    #region IPubSub Members

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(PubSubPublishRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publish request");

        var queue = _queues.AddOrUpdate(request.Topic, _ => new ConcurrentBag<PubSubPublishRequest>(), (_, __) => __);

        queue.Add(request);

        return Task.CompletedTask;
    }

    private readonly ConcurrentDictionary<string, PullMessagesHandler> _handlers = new ConcurrentDictionary<string, PullMessagesHandler>();

    private sealed record PullMessagesHandler(MessageDeliveryHandler<string?, PubSubPullMessagesResponse> Handler, CancellationToken CancellationToken);

    public async Task PullMessagesAsync(PubSubPullMessagesTopic topic, MessageDeliveryHandler<string?, PubSubPullMessagesResponse> handler, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pull messages request for topic \"{0}\"", topic.Name);

        var handlerRecord = new PullMessagesHandler(handler, cancellationToken);

        _handlers.AddOrUpdate(topic.Name, handlerRecord, (_, __) => handlerRecord);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_queues.TryGetValue(topic.Name, out var queue))
            {
                while (!cancellationToken.IsCancellationRequested && queue.TryTake(out var request))
                {
                    try
                    {
                        await handler(
                            new PubSubPullMessagesResponse(request.Topic)
                            {
                                Data = request.Data.ToArray()
                            },
                            error =>
                            {
                                if (error is not null)
                                {
                                    queue.Add(request);
                                }
                                
                                return Task.CompletedTask;
                            }).ConfigureAwait(false);
                    }
                    catch
                    {
                        queue.Add(request);

                        throw;
                    }
                }
            }

            await Task.Delay(_pollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion
}
