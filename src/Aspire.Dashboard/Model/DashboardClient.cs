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
internal sealed class DashboardClient(ILogger<DashboardClient> logger) : IDashboardClient
{
    private const string DashboardServiceUrlVariableName = "DOTNET_DASHBOARD_GRPC_ENDPOINT_URL";
    private const string DashboardServiceUrlDefaultValue = "http://localhost:18999";

    private readonly Dictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();
    private readonly ILogger<DashboardClient> _logger = logger;

    private ImmutableHashSet<Channel<ResourceViewModelChange>> _outgoingChannels = [];
    private string? _applicationName;
    private string? _applicationVersion;

    private const int StateNone = 0;
    private const int StateInitialized = 1;
    private const int StateDisposed = 2;
    private int _state;

    private TaskCompletionSource<DashboardService.DashboardServiceClient> _client = new();

    private void EnsureInitialized()
    {
        var priorState = Interlocked.CompareExchange(ref _state, StateInitialized, comparand: StateNone);

        if (priorState is not StateNone)
        {
            ObjectDisposedException.ThrowIf(priorState is StateDisposed, this);
            return;
        }

        var address = GetAddressUri(DashboardServiceUrlVariableName, DashboardServiceUrlDefaultValue);

        _ = Task.Run(() => ConnectAndStayConnectedAsync(address, _cts.Token), _cts.Token);

        async Task ConnectAndStayConnectedAsync(Uri address, CancellationToken cancellationToken)
        {
            GrpcChannel? channel = null;
            DashboardService.DashboardServiceClient? client = null;

            var errorCount = 0;

            try
            {
                (channel, client) = await ConnectAsync().ConfigureAwait(false);

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (channel.State == ConnectivityState.Shutdown)
                    {
                        // Recreate connection
                        _logger.LogWarning("Lost connection to dashboard service. Reconnecting.");

                        channel.Dispose();
                        Debug.Assert(_client.Task.IsCompleted);
                        _client = new();

                        (channel, client) = await ConnectAsync().ConfigureAwait(false);
                    }

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
                        await ProcessData().ConfigureAwait(false);

                        errorCount = 0;
                    }
                    catch (RpcException ex)
                    {
                        errorCount++;

                        _logger.LogError("Error {errorCount} watching resources: {error}", errorCount, ex.Message);
                    }
                }
            }
            finally
            {
                channel?.Dispose();
            }

            return;

            async Task<(GrpcChannel, DashboardService.DashboardServiceClient)> ConnectAsync()
            {
                _logger.LogInformation("Connecting to dashboard service at: {address}", address);

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

                await channel.ConnectAsync(cancellationToken).ConfigureAwait(false);

                var response = await client.GetApplicationInformationAsync(new(), cancellationToken: cancellationToken);

                _applicationName = response.ApplicationName;
                _applicationVersion = response.ApplicationVersion;

                _client.SetResult(client);

                return (channel, client);
            }

            static TimeSpan ExponentialBackOff(int errorCount, double maxSeconds)
            {
                return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, errorCount - 1), maxSeconds));
            }

            async Task ProcessData()
            {
                var call = client.WatchResources(new WatchResourcesRequest { IsReconnect = errorCount != 0 }, cancellationToken: cancellationToken);

                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                {
                    List<ResourceViewModelChange>? changes = null;

                    lock (_lock)
                    {
                        // The most reliable way to check that a streaming call succeeded is to successfully read a response.
                        if (errorCount > 0)
                        {
                            _resourceByName.Clear();
                            errorCount = 0;
                        }

                        if (response.KindCase == WatchResourcesUpdate.KindOneofCase.InitialData)
                        {
                            // Copy initial snapshot into model, and send to any subscribers that exist.
                            // TODO we need to send a "clear" event via outgoing channels, in case they already have state
                            foreach (var resource in response.InitialData.Resources)
                            {
                                changes ??= [];

                                var viewModel = resource.ToViewModel();
                                _resourceByName[resource.Name] = viewModel;
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
                            }
                        }
                        else
                        {
                            throw new FormatException("Unsupported response kind: " + response.KindCase);
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
    }

    public string ApplicationName => _applicationName ?? "";

    ResourceViewModelSubscription IDashboardClient.SubscribeResources()
    {
        EnsureInitialized();

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

            async IAsyncEnumerable<ResourceViewModelChange> StreamUpdates()
            {
                try
                {
                    await foreach (var batch in channel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
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

    public async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureInitialized();

        var client = await _client.Task.ConfigureAwait(false);

        var call = client.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest() { ResourceName = resourceName },
            cancellationToken: cancellationToken);

        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
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
            await _cts.CancelAsync().ConfigureAwait(false);

            _cts.Dispose();
        }
    }
}
