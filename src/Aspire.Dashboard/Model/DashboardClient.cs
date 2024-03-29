// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
/// <see cref="IDashboardClient.SubscribeResourcesAsync"/> and <see cref="IDashboardClient.SubscribeConsoleLogs"/>
/// will throw if <see cref="IDashboardClient.IsEnabled"/> is <see langword="false"/>. Callers should
/// check this property first, before calling these methods.
/// </para>
/// </remarks>
internal sealed class DashboardClient : IDashboardClient
{
    private const string ResourceServiceUrlVariableName = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL";

    private readonly Dictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _clientCancellationToken;
    private readonly TaskCompletionSource _whenConnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _initialDataReceivedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _lock = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DashboardClient> _logger;

    private ImmutableHashSet<Channel<IReadOnlyList<ResourceViewModelChange>>> _outgoingChannels = [];
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

        // Take a copy of the token and always use it to avoid race between disposal of CTS and usage of token.
        _clientCancellationToken = _cts.Token;

        _logger = loggerFactory.CreateLogger<DashboardClient>();

        var address = configuration.GetUri(ResourceServiceUrlVariableName);

        if (address is null)
        {
            _state = StateDisabled;
            _logger.LogDebug($"{ResourceServiceUrlVariableName} is not specified. Dashboard client services are unavailable.");
            _cts.Cancel();
            _whenConnectedTcs.TrySetCanceled();
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

            var authMode = configuration.GetEnum<ResourceClientAuthMode>("ResourceServiceClient:AuthMode");

            if (authMode == ResourceClientAuthMode.Certificate)
            {
                // Auth hasn't been suppressed, so configure it.
                var sourceType = configuration.GetEnum<DashboardClientCertificateSource>("ResourceServiceClient:ClientCertificate:Source");

                var certificates = sourceType switch
                {
                    DashboardClientCertificateSource.File => GetFileCertificate(),
                    DashboardClientCertificateSource.KeyStore => GetKeyStoreCertificate(),
                    _ => throw new InvalidOperationException("Unable to load ResourceServiceClient client certificate.")
                };

                httpHandler.SslOptions = new SslClientAuthenticationOptions
                {
                    ClientCertificates = certificates
                };

                configuration.Bind("ResourceServiceClient:Ssl", httpHandler.SslOptions);
            }

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

            X509CertificateCollection GetFileCertificate()
            {
                var filePath = configuration["ResourceServiceClient:ClientCertificate:FilePath"];
                var password = configuration["ResourceServiceClient:ClientCertificate:Password"];

                if (filePath is null or [])
                {
                    throw new InvalidOperationException("ResourceServiceClient:ClientCertificate:Source is \"File\", but no Certificate:FilePath is configured.");
                }

                return [new X509Certificate2(filePath, password)];
            }

            X509CertificateCollection GetKeyStoreCertificate()
            {
                var subject = configuration["ResourceServiceClient:ClientCertificate:Subject"];

                if (subject is null or [])
                {
                    throw new InvalidOperationException("ResourceServiceClient:ClientCertificate:Source is \"KeyStore\", but no Certificate:FilePath is configured.");
                }

                var storeProperties = new KeyStoreProperties { Name = "My", Location = StoreLocation.CurrentUser };

                configuration.Bind("ResourceServiceClient:ClientCertificate:KeyStore", storeProperties);

                using var store = new X509Store(storeName: storeProperties.Name, storeLocation: storeProperties.Location);

                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, findValue: subject, validOnly: true);

                if (certificates is [])
                {
                    throw new InvalidOperationException($"Unable to load client certificate with subject \"{subject}\" from key store.");
                }

                return certificates;
            }
        }
    }

    internal sealed class KeyStoreProperties
    {
        public required string Name { get; set; }
        public required StoreLocation Location { get; set; }
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

        _connection = Task.Run(() => ConnectAndWatchResourcesAsync(_clientCancellationToken), _clientCancellationToken);

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

                    _whenConnectedTcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    _whenConnectedTcs.TrySetException(ex);
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

                    await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken).ConfigureAwait(true)) // Setting ConfigureAwait to silence analyzer. Consider calling ConfigureAwait(false)
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

                                _initialDataReceivedTcs.TrySetResult();
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
                                // Channel is unbound so TryWrite always succeeds.
                                channel.Writer.TryWrite(changes);
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

            return _whenConnectedTcs.Task;
        }
    }

    string IDashboardClient.ApplicationName
    {
        get => _applicationName
            ?? _configuration["DOTNET_DASHBOARD_APPLICATION_NAME"]
            ?? "Aspire";
    }

    async Task<ResourceViewModelSubscription> IDashboardClient.SubscribeResourcesAsync(CancellationToken cancellationToken)
    {
        EnsureInitialized();

        var cts = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

        // Wait for initial data to be received from the server. This allows initial data to be returned with subscription when client is starting.
        await _initialDataReceivedTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);

        // There are two types of channel in this class. This is not a gRPC channel.
        // It's a producer-consumer queue channel, used to push updates to subscribers
        // without blocking the producer here.
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceViewModelChange>>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

        lock (_lock)
        {
            ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Add(channel), channel);

            return new ResourceViewModelSubscription(
                InitialState: _resourceByName.Values.ToImmutableArray(),
                Subscription: StreamUpdatesAsync(cts.Token));
        }

        async IAsyncEnumerable<IReadOnlyList<ResourceViewModelChange>> StreamUpdatesAsync([EnumeratorCancellation] CancellationToken enumeratorCancellationToken = default)
        {
            try
            {
                await foreach (var batch in channel.GetBatchesAsync(minReadInterval: TimeSpan.FromMilliseconds(100), cancellationToken: enumeratorCancellationToken).ConfigureAwait(false))
                {
                    if (batch.Count == 1)
                    {
                        yield return batch[0];
                    }
                    else
                    {
                        yield return batch.SelectMany(batch => batch).ToList();
                    }
                }
            }
            finally
            {
                cts.Dispose();
                ImmutableInterlocked.Update(ref _outgoingChannels, static (set, channel) => set.Remove(channel), channel);
            }
        }
    }

    async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? IDashboardClient.SubscribeConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureInitialized();

        using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

        var call = _client!.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest() { ResourceName = resourceName },
            cancellationToken: combinedTokens.Token);

        // Write incoming logs to a channel, and then read from that channel to yield the logs.
        // We do this to batch logs together and enforce a minimum read interval.
        var channel = Channel.CreateUnbounded<IReadOnlyList<(string Content, bool IsErrorMessage)>>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: combinedTokens.Token).ConfigureAwait(true)) // Setting ConfigureAwait to silence analyzer. Consider calling ConfigureAwait(false)
                {
                    var logLines = new (string Content, bool IsErrorMessage)[response.LogLines.Count];

                    for (var i = 0; i < logLines.Length; i++)
                    {
                        logLines[i] = (response.LogLines[i].Text, response.LogLines[i].IsStdErr);
                    }

                    // Channel is unbound so TryWrite always succeeds.
                    channel.Writer.TryWrite(logLines);
                }
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, combinedTokens.Token);

        await foreach (var batch in channel.GetBatchesAsync(TimeSpan.FromMilliseconds(100), combinedTokens.Token).ConfigureAwait(true)) // Setting ConfigureAwait to silence analyzer. Consider calling ConfigureAwait(false)
        {
            if (batch.Count == 1)
            {
                yield return batch[0];
            }
            else
            {
                yield return batch.SelectMany(batch => batch).ToList();
            }
        }

        await readTask.ConfigureAwait(false);
    }

    public async Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken)
    {
        EnsureInitialized();

        var request = new ResourceCommandRequest()
        {
            CommandType = command.CommandType,
            Parameter = command.Parameter,
            ResourceName = resourceName,
            ResourceType = resourceType
        };

        try
        {
            using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

            var response = await _client!.ExecuteResourceCommandAsync(request, cancellationToken: combinedTokens.Token);

            return response.ToViewModel();
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error executing command \"{CommandType}\" on resource \"{ResourceName}\": {StatusCode}", command.CommandType, resourceName, ex.StatusCode);

            var errorMessage = ex.StatusCode == StatusCode.Unimplemented ? "Command not implemented" : "Unknown error. See logs for details";

            return new ResourceCommandResponseViewModel()
            {
                Kind = ResourceCommandResponseKind.Failed,
                ErrorMessage = errorMessage
            };
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

    // Internal for testing.
    // TODO: Improve this in the future by making the client injected with DI and have it return data.
    internal void SetInitialDataReceived(IList<Resource>? initialData = null)
    {
        if (initialData != null)
        {
            lock (_lock)
            {
                foreach (var data in initialData)
                {
                    _resourceByName[data.Name] = data.ToViewModel();
                }
            }
        }

        _initialDataReceivedTcs.TrySetResult();
    }

    private enum DashboardClientCertificateSource
    {
        File,
        KeyStore
    }

    private enum ResourceClientAuthMode
    {
        Unsecured,
        Certificate
    }
}
