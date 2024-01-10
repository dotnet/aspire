// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Aspire.Dashboard.Utils;
using Aspire.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Implements gRPC client that communicates with a resource server, populating data for the dashboard.
/// </summary>
/// <remarks>
/// An instance of this type is created per service call, so this class should not hold onto any state
/// expected to live longer than a single RPC request. In the case of streaming requests, the instance
/// lives until the stream is closed.
/// </remarks>
internal sealed class DashboardClient : IDashboardClient
{
    private const string DashboardServiceUrlVariableName = "DOTNET_DASHBOARD_GRPC_ENDPOINT_URL";
    private const string DashboardServiceUrlDefaultValue = "http://localhost:18999";

    private readonly Dictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _whenConnected = new();
    private readonly object _lock = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DashboardClient> _logger;

    private ImmutableHashSet<Channel<ResourceViewModelChange>> _outgoingChannels = [];
    private string? _applicationName;

    private const int StateNone = 0;
    private const int StateInitialized = 1;
    private const int StateDisposed = 2;
    private int _state;

    private readonly GrpcChannel _channel;
    private readonly DashboardService.DashboardServiceClient _client;

    private Task? _connection;

    public DashboardClient(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;

        _logger = loggerFactory.CreateLogger<DashboardClient>();

        var address = GetAddressUri(DashboardServiceUrlVariableName, DashboardServiceUrlDefaultValue);

        _logger.LogInformation("Dashboard configured to connect to: {Address}", address);

        // Create the gRPC channel. This channel performs automatic reconnects.
        // We will dispose it when we are disposed.
        _channel = CreateChannel();

        _client = new DashboardService.DashboardServiceClient(_channel);

        static Uri GetAddressUri(string variableName, string defaultValue)
        {
            try
            {
                var uri = Environment.GetEnvironmentVariable(variableName) ?? defaultValue;

                return new Uri(uri);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error parsing URIs from environment variable '{variableName}'.", ex);
            }
        }

        GrpcChannel CreateChannel()
        {
            var httpHandler = new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
                KeepAlivePingDelay = TimeSpan.FromSeconds(20),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests
            };

            // https://learn.microsoft.com/aspnet/core/grpc/retries

            var methodConfig = new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromSeconds(1),
                    MaxBackoff = TimeSpan.FromSeconds(5),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { StatusCode.Unavailable }
                }
            };

            // https://learn.microsoft.com/aspnet/core/grpc/diagnostics#grpc-client-logging

            return GrpcChannel.ForAddress(
                address,
                channelOptions: new()
                {
                    HttpHandler = httpHandler,
                    ServiceConfig = new() { MethodConfigs = { methodConfig } },
                    LoggerFactory = _loggerFactory,
                    ThrowOperationCanceledOnCancellation = true
                });
        }
    }

    // For testing purposes
    internal int OutgoingResourceSubscriberCount => _outgoingChannels.Count;

    private void EnsureInitialized()
    {
        var priorState = Interlocked.CompareExchange(ref _state, StateInitialized, comparand: StateNone);

        if (priorState is not StateNone)
        {
            ObjectDisposedException.ThrowIf(priorState is StateDisposed, this);
            return;
        }

        _connection = Task.Run(() => ConnectAndWatchResourcesAsync(_cts.Token), _cts.Token);

        return;

        async Task ConnectAndWatchResourcesAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync().ConfigureAwait(false);

            await WatchResourcesWithRecoveryAsync().ConfigureAwait(false);

            async Task ConnectAsync()
            {
                try
                {
                    var response = await _client.GetApplicationInformationAsync(new(), cancellationToken: cancellationToken);

                    _applicationName = response.ApplicationName;

                    _whenConnected.TrySetResult();
                }
                catch (Exception ex)
                {
                    _whenConnected.TrySetException(ex);
                }
            }

            async Task WatchResourcesWithRecoveryAsync()
            {
                // Track the number of errors we've seen since the last successfully received message.
                // As this number climbs, we extend the amount of time between reconnection attempts, in
                // order to avoid flooding the server with requests. This value is reset to zero whenever
                // a message is successfully received.
                var errorCount = 0;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (errorCount > 0)
                    {
                        // The most recent attempt failed. There may be more than one failure.
                        // We wait for a period of time determined by the number of errors,
                        // where the time grows exponentially, until a threshold.
                        var delay = ExponentialBackOff(errorCount, maxSeconds: 15);

                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }

                    try
                    {
                        await WatchResourcesAsync().ConfigureAwait(false);
                    }
                    catch (RpcException ex)
                    {
                        errorCount++;

                        _logger.LogError(ex, "Error #{ErrorCount} watching resources.", errorCount);
                    }
                }

                static TimeSpan ExponentialBackOff(int errorCount, double maxSeconds)
                {
                    return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, errorCount - 1), maxSeconds));
                }

                async Task WatchResourcesAsync()
                {
                    var call = _client.WatchResources(new WatchResourcesRequest { IsReconnect = errorCount != 0 }, cancellationToken: cancellationToken);

                    await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                    {
                        List<ResourceViewModelChange>? changes = null;

                        lock (_lock)
                        {
                            // We received a message, which means we are connected. Clear the error count.
                            errorCount = 0;

                            if (response.KindCase == WatchResourcesUpdate.KindOneofCase.InitialData)
                            {
                                // Populate our map using the initial data.
                                _resourceByName.Clear();

                                // TODO send a "clear" event via outgoing channels, in case consumers have extra items to be removed

                                foreach (var resource in response.InitialData.Resources)
                                {
                                    // Add to map.
                                    var viewModel = resource.ToViewModel();
                                    _resourceByName[resource.Name] = viewModel;

                                    // Send this update to any subscribers too.
                                    changes ??= [];
                                    changes.Add(new(ResourceViewModelChangeType.Upsert, viewModel));
                                }
                            }
                            else if (response.KindCase == WatchResourcesUpdate.KindOneofCase.Changes)
                            {
                                // Apply changes to the model.
                                foreach (var change in response.Changes.Value)
                                {
                                    changes ??= [];

                                    if (change.KindCase == WatchResourcesChange.KindOneofCase.Upsert)
                                    {
                                        // Upsert (i.e. add or replace)
                                        var viewModel = change.Upsert.ToViewModel();
                                        _resourceByName[change.Upsert.Name] = viewModel;
                                        changes.Add(new(ResourceViewModelChangeType.Upsert, viewModel));
                                    }
                                    else if (change.KindCase == WatchResourcesChange.KindOneofCase.Delete)
                                    {
                                        // Remove
                                        if (_resourceByName.Remove(change.Delete.ResourceName, out var removed))
                                        {
                                            changes.Add(new(ResourceViewModelChangeType.Delete, removed));
                                        }
                                        else
                                        {
                                            Debug.Fail("Attempt to remove an unknown resource view model.");
                                        }
                                    }
                                    else
                                    {
                                        throw new FormatException($"Unexpected {nameof(WatchResourcesChange)} kind: {change.KindCase}");
                                    }
                                }
                            }
                            else
                            {
                                throw new FormatException($"Unexpected {nameof(WatchResourcesUpdate)} kind: {response.KindCase}");
                            }
                        }

                        if (changes is not null)
                        {
                            foreach (var channel in _outgoingChannels)
                            {
                                // TODO send batches over the channel instead of individual items? They are batched downstream however
                                foreach (var change in changes)
                                {
                                    await channel.Writer.WriteAsync(change, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    Task IDashboardClient.WhenConnected => _whenConnected.Task;

    string IDashboardClient.ApplicationName => _applicationName ?? "";

    ResourceViewModelSubscription IDashboardClient.SubscribeResources()
    {
        EnsureInitialized();

        var clientCancellationToken = _cts.Token;

        lock (_lock)
        {
            // There are two types of channel in this class. This is not a gRPC channel.
            // It's a producer-consumer queue channel, used to push updates to subscribers
            // without blocking the producer here.
            var channel = Channel.CreateUnbounded<ResourceViewModelChange>(
                new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            return new ResourceViewModelSubscription(
                InitialState: _resourceByName.Values.ToImmutableArray(),
                Subscription: StreamUpdates());

            async IAsyncEnumerable<ResourceViewModelChange> StreamUpdates([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(clientCancellationToken, enumeratorCancellationToken);

                    await foreach (var batch in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
                    {
                        yield return batch;
                    }
                }
                finally
                {
                    ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
                }
            }
        }
    }

    async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? IDashboardClient.SubscribeConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureInitialized();

        using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellationToken);

        var call = _client.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest() { ResourceName = resourceName },
            cancellationToken: combinedTokens.Token);

        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: combinedTokens.Token))
        {
            var logLines = new (string Content, bool IsErrorMessage)[response.LogLines.Count];

            for (var i = 0; i < logLines.Length; i++)
            {
                logLines[i] = (response.LogLines[i].Text, response.LogLines[i].IsStdErr);
            }

            yield return logLines;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _state, StateDisposed) is not StateDisposed)
        {
            _outgoingChannels = [];

            await _cts.CancelAsync().ConfigureAwait(false);

            _cts.Dispose();

            _channel.Dispose();

            try
            {
                if (_connection is { IsCanceled: false })
                {
                    await _connection.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error from connection task.");
            }
        }
    }
}
