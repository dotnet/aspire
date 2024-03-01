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

namespace Aspire.Dashboard.Model;

/// <summary>
/// Implements gRPC client that communicates with a resource server, populating data for the dashboard.
/// </summary>
/// <remarks>
/// <para>
/// An instance of this type is created per service call, so this class should not hold onto any state
/// expected to live longer than a single RPC request. In the case of streaming requests, the instance
/// lives until the stream is closed.
/// </para>
/// <para>
/// If the <c>DOTNET_RESOURCE_SERVICE_ENDPOINT_URL</c> environment variable is not specified, then there's
/// no known endpoint to connect to, and this dashboard client will be disabled. Calls to
/// <see cref="IDashboardClient.SubscribeResources"/> and <see cref="IDashboardClient.SubscribeConsoleLogs"/>
/// will throw if <see cref="IDashboardClient.IsEnabled"/> is <see langword="false"/>. Callers should
/// check this property first, before calling these methods.
/// </para>
/// </remarks>
internal sealed class DashboardClient : IDashboardClient
{
    private const string ResourceServiceUrlVariableName = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL";

    private readonly Dictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly TaskCompletionSource _whenConnected = new();
    private readonly object _lock = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardClient> _logger;

    private ImmutableHashSet<Channel<ResourceViewModelChange>> _outgoingChannels = [];
    private string? _applicationName;

    private const int StateDisabled = -1;
    private const int StateNone = 0;
    private const int StateInitialized = 1;
    private const int StateDisposed = 2;
    private int _state = StateNone;

    private readonly GrpcChannel? _channel;
    private readonly DashboardService.DashboardServiceClient? _client;

    private Task? _connection;

    public DashboardClient(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;

        _logger = loggerFactory.CreateLogger<DashboardClient>();

        var address = configuration.GetUri(ResourceServiceUrlVariableName);

        if (address is null)
        {
            _state = StateDisabled;
            _logger.LogDebug($"{ResourceServiceUrlVariableName} is not specified. Dashboard client services are unavailable.");
            _cts.Cancel();
            _whenConnected.TrySetCanceled();
            return;
        }

        _logger.LogDebug("Dashboard configured to connect to: {Address}", address);

        // Create the gRPC channel. This channel performs automatic reconnects.
        // We will dispose it when we are disposed.
        _channel = CreateChannel();

        _client = new DashboardService.DashboardServiceClient(_channel);

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

    public bool IsEnabled => _state is not StateDisabled;

    private void EnsureInitialized()
    {
        var priorState = Interlocked.CompareExchange(ref _state, value: StateInitialized, comparand: StateNone);

        if (priorState is StateDisabled)
        {
            throw new InvalidOperationException($"{nameof(DashboardClient)} is disabled. Check the {nameof(IsEnabled)} property before calling this.");
        }

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
                    var response = await _client!.GetApplicationInformationAsync(new(), cancellationToken: cancellationToken);

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
                    var call = _client!.WatchResources(new WatchResourcesRequest { IsReconnect = errorCount != 0 }, cancellationToken: cancellationToken);

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

    Task IDashboardClient.WhenConnected
    {
        get
        {
            // All pages wait for this task (it is used to display the title) but some don't subscribe to resources.
            // If someone is waiting for the connection, we need to ensure connection is starting.
            EnsureInitialized();

            return _whenConnected.Task;
        }
    }

    string IDashboardClient.ApplicationName
    {
        get => _applicationName
            ?? _configuration["DOTNET_DASHBOARD_APPLICATION_NAME"]
            ?? "Aspire";
    }

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

        var call = _client!.WatchResourceConsoleLogs(
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

            _channel?.Dispose();

            await TaskHelpers.WaitIgnoreCancelAsync(_connection, _logger, "Unexpected error from connection task.").ConfigureAwait(false);
        }
    }
}
