// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public sealed class DashboardTelemetrySender : IDashboardTelemetrySender
{
    private readonly IOptions<DashboardOptions> _options;
    private readonly ILogger<DashboardTelemetrySender> _logger;
    private readonly Channel<(OperationContext, Func<HttpClient, Func<OperationContextProperty, object>, Task>)> _channel;
    private bool? _isEnabled;
    private Task? _sendLoopTask;

    // Internal for testing.
    internal Func<HttpClientHandler, HttpMessageHandler>? CreateHandler { get; set; }
    internal HttpClient? Client { get; private set; }

    public DashboardTelemetrySender(IOptions<DashboardOptions> options, ILogger<DashboardTelemetrySender> logger)
    {
        _options = options;
        _logger = logger;
        var channelOptions = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        };
        _channel = Channel.CreateBounded<(OperationContext, Func<HttpClient, Func<OperationContextProperty, object>, Task>)>(channelOptions);
    }

    private void StartSendLoop()
    {
        Debug.Assert(Client is not null, "HttpClient must be initialized.");

        _sendLoopTask = Task.Run(async () =>
        {
            _logger.LogInformation("Starting sender loop.");

            try
            {
                while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
                {
                    while (_channel.Reader.TryRead(out var operation))
                    {
                        var (context, requestFunc) = operation;
                        try
                        {
                            await requestFunc(Client, GetResponseProperty).ConfigureAwait(false);

                            _logger.LogTrace("Telemetry request {Name} successfully sent.", context.Name);

                            // Double check properties are set.
                            foreach (var property in context.Properties)
                            {
                                if (!property.HasValue)
                                {
                                    _logger.LogWarning("Unset context property in request {Name}.", context.Name);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send telemetry request {Name}.", context.Name);
                        }
                    }
                }
            }
            finally
            {
                _logger.LogInformation("Ending sender loop.");
            }
        });
    }

    private object GetResponseProperty(OperationContextProperty propertyId)
    {
        return propertyId.GetValue();
    }

    public async Task<bool> TryStartTelemetrySessionAsync()
    {
        Debug.Assert(_isEnabled is null, "Telemetry session has already been started.");

        _isEnabled = await TryStartTelemetrySessionCoreAsync().ConfigureAwait(false);
        if (_isEnabled.Value)
        {
            StartSendLoop();
        }

        return _isEnabled.Value;
    }

    internal bool CreateHttpClient()
    {
        if (DebugSessionHelpers.HasDebugSession(_options.Value.DebugSession, out var certificate, out var debugSessionUri, out var token))
        {
            if (_options.Value.DebugSession.TelemetryOptOut is not true)
            {
                Client = DebugSessionHelpers.CreateHttpClient(debugSessionUri, token, certificate, CreateHandler);
                return true;
            }
        }

        _logger.LogInformation("Telemetry is not configured.");
        return false;
    }

    private async Task<bool> TryStartTelemetrySessionCoreAsync()
    {
        if (!CreateHttpClient())
        {
            return false;
        }

        Debug.Assert(Client is not null);

        try
        {
            var response = await Client.GetAsync(TelemetryEndpoints.TelemetryEnabled).ConfigureAwait(false);
            var isTelemetryEnabled = response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };

            if (!isTelemetryEnabled)
            {
                return false;
            }

            // start the actual telemetry session
            var telemetryStartedStatusCode = (await Client.PostAsync(TelemetryEndpoints.TelemetryStart, content: null).ConfigureAwait(false)).StatusCode;
            return telemetryStartedStatusCode is HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            // If the request fails, we've been given an invalid server address and should assume telemetry is unsupported.
            _logger.LogWarning(ex, "Failed to request whether telemetry is supported.");
            return false;
        }
    }

    public void QueueRequest(OperationContext context, Func<HttpClient, Func<OperationContextProperty, object>, Task> requestFunc)
    {
        if (Client is null)
        {
            return;
        }

        _channel.Writer.TryWrite((context, requestFunc));
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        if (_sendLoopTask is { } task)
        {
            await task.ConfigureAwait(false);
        }

        Client?.Dispose();
    }
}
