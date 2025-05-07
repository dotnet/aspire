// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Utils;
using Aspire.Hosting;
using Aspire.ResourceService.Proto.V1;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Options;

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
    private const string ApiKeyHeaderName = "x-resource-service-api-key";

    private readonly Dictionary<string, ResourceViewModel> _resourceByName = new(StringComparers.ResourceName);
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _clientCancellationToken;
    private readonly TaskCompletionSource _whenConnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _initialDataReceivedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _lock = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly IKnownPropertyLookup _knownPropertyLookup;
    private readonly DashboardOptions _dashboardOptions;
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
    private readonly Metadata _headers = [];

    private Task? _connection;

    public DashboardClient(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        IOptions<DashboardOptions> dashboardOptions,
        IKnownPropertyLookup knownPropertyLookup,
        Action<SocketsHttpHandler>? configureHttpHandler = null)
    {
        _loggerFactory = loggerFactory;
        _knownPropertyLookup = knownPropertyLookup;
        _dashboardOptions = dashboardOptions.Value;

        // Take a copy of the token and always use it to avoid race between disposal of CTS and usage of token.
        _clientCancellationToken = _cts.Token;

        _logger = loggerFactory.CreateLogger<DashboardClient>();

        if (dashboardOptions.Value.ResourceServiceClient.GetUri() is null)
        {
            _state = StateDisabled;
            _logger.LogDebug($"{DashboardConfigNames.ResourceServiceUrlName.ConfigKey} is not specified. Dashboard client services are unavailable.");
            _cts.Cancel();
            _whenConnectedTcs.TrySetCanceled();
            return;
        }

        var address = _dashboardOptions.ResourceServiceClient.GetUri()!;
        _logger.LogDebug("Dashboard configured to connect to: {Address}", address);

        // Create the gRPC channel. This channel performs automatic reconnects.
        // We will dispose it when we are disposed.
        _channel = CreateChannel();

        if (_dashboardOptions.ResourceServiceClient.AuthMode is ResourceClientAuthMode.ApiKey)
        {
            // We're using an API key for auth, so set it in the headers we pass on each call.
            _headers.Add(ApiKeyHeaderName, _dashboardOptions.ResourceServiceClient.ApiKey!);
        }

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

            var authMode = _dashboardOptions.ResourceServiceClient.AuthMode;

            if (authMode == ResourceClientAuthMode.Certificate)
            {
                // Auth hasn't been suppressed, so configure it.
                var certificates = _dashboardOptions.ResourceServiceClient.ClientCertificate.Source switch
                {
                    DashboardClientCertificateSource.File => GetFileCertificate(),
                    DashboardClientCertificateSource.KeyStore => GetKeyStoreCertificate(),
                    _ => throw new InvalidOperationException("Unable to load ResourceServiceClient client certificate.")
                };

                httpHandler.SslOptions = new SslClientAuthenticationOptions
                {
                    ClientCertificates = certificates
                };

                configuration.Bind("Dashboard:ResourceServiceClient:Ssl", httpHandler.SslOptions);
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

            configureHttpHandler?.Invoke(httpHandler);

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
                Debug.Assert(
                    _dashboardOptions.ResourceServiceClient.ClientCertificate.FilePath != null,
                    "FilePath is validated as not null when configuration is loaded.");

                var filePath = _dashboardOptions.ResourceServiceClient.ClientCertificate.FilePath;
                var password = _dashboardOptions.ResourceServiceClient.ClientCertificate.Password;

                return [new X509Certificate2(filePath, password)];
            }

            X509CertificateCollection GetKeyStoreCertificate()
            {
                Debug.Assert(
                    _dashboardOptions.ResourceServiceClient.ClientCertificate.Subject != null,
                    "Subject is validated as not null when configuration is loaded.");

                var subject = _dashboardOptions.ResourceServiceClient.ClientCertificate.Subject;
                var storeName = _dashboardOptions.ResourceServiceClient.ClientCertificate.Store ?? "My";
                var location = _dashboardOptions.ResourceServiceClient.ClientCertificate.Location ?? StoreLocation.CurrentUser;

                using var store = new X509Store(storeName: storeName, storeLocation: location);

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
            try
            {
                await ConnectAsync().ConfigureAwait(false);

                await WatchResourcesWithRecoveryAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Ignore. This is likely caused by the dashboard client being disposed. We don't want to log.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from the resource service.");
                throw;
            }

            async Task ConnectAsync()
            {
                try
                {
                    var response = await _client!.GetApplicationInformationAsync(new(), headers: _headers, cancellationToken: cancellationToken);

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
                    catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                    {
                        // There's a race condition between reconnect attempts and client disposal.
                        // This has been observed in unit tests where the client is created and disposed
                        // very quickly. This check should probably be in the gRPC library instead.
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
                    var call = _client!.WatchResources(new WatchResourcesRequest { IsReconnect = errorCount != 0 }, headers: _headers, cancellationToken: cancellationToken);

                    await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
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
                                    var viewModel = resource.ToViewModel(_knownPropertyLookup, _logger);
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
                                        var viewModel = change.Upsert.ToViewModel(_knownPropertyLookup, _logger);
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
            ?? _dashboardOptions.ApplicationName
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

    async IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> IDashboardClient.SubscribeConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureInitialized();

        using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

        var call = _client!.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest() { ResourceName = resourceName },
            headers: _headers,
            cancellationToken: combinedTokens.Token);

        // Write incoming logs to a channel, and then read from that channel to yield the logs.
        // We do this to batch logs together and enforce a minimum read interval.
        var channel = Channel.CreateUnbounded<IReadOnlyList<ResourceLogLine>>(
            new UnboundedChannelOptions { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = true });

        var readTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: combinedTokens.Token).ConfigureAwait(false))
                {
                    // Channel is unbound so TryWrite always succeeds.
                    channel.Writer.TryWrite(CreateLogLines(response.LogLines));
                }
            }
            finally
            {
                channel.Writer.TryComplete();
            }
        }, combinedTokens.Token);

        await foreach (var batch in channel.GetBatchesAsync(TimeSpan.FromMilliseconds(100), combinedTokens.Token).ConfigureAwait(false))
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

    async IAsyncEnumerable<IReadOnlyList<ResourceLogLine>> IDashboardClient.GetConsoleLogs(string resourceName, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureInitialized();

        using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

        var call = _client!.WatchResourceConsoleLogs(
            new WatchResourceConsoleLogsRequest() { ResourceName = resourceName, SuppressFollow = true },
            headers: _headers,
            cancellationToken: combinedTokens.Token);

        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken: combinedTokens.Token).ConfigureAwait(false))
        {
            yield return CreateLogLines(response.LogLines);
        }
    }

    private static ResourceLogLine[] CreateLogLines(IList<ConsoleLogLine> logLines)
    {
        var resourceLogLines = new ResourceLogLine[logLines.Count];

        for (var i = 0; i < logLines.Count; i++)
        {
            resourceLogLines[i] = new ResourceLogLine(logLines[i].LineNumber, logLines[i].Text, logLines[i].IsStdErr);
        }

        return resourceLogLines;
    }

    public async Task<ResourceCommandResponseViewModel> ExecuteResourceCommandAsync(string resourceName, string resourceType, CommandViewModel command, CancellationToken cancellationToken)
    {
        EnsureInitialized();

        var request = new ResourceCommandRequest()
        {
            CommandName = command.Name,
            Parameter = command.Parameter,
            ResourceName = resourceName,
            ResourceType = resourceType
        };

        try
        {
            using var combinedTokens = CancellationTokenSource.CreateLinkedTokenSource(_clientCancellationToken, cancellationToken);

            var response = await _client!.ExecuteResourceCommandAsync(request, headers: _headers, cancellationToken: combinedTokens.Token);

            return response.ToViewModel();
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Error executing command \"{CommandName}\" on resource \"{ResourceName}\": {StatusCode}", command.Name, resourceName, ex.StatusCode);

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
                    _resourceByName[data.Name] = data.ToViewModel(_knownPropertyLookup, _logger);
                }
            }
        }

        _initialDataReceivedTcs.TrySetResult();
    }
}
