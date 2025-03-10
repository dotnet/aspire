// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public class DashboardTelemetrySender : IDashboardTelemetrySender
{
    private readonly IOptions<DashboardOptions> _options;
    private readonly ILogger<DashboardTelemetrySender> _logger;
    private readonly Channel<(OperationContext, Func<HttpClient, Func<OperationContextProperty, object>, Task>)> _channel;
    private HttpClient? _httpClient;
    private bool? _isEnabled;
    private Task? _sendLoopTask;

    // Internal for testing.
    internal Func<HttpClientHandler, HttpMessageHandler>? CreateHandler { get; set; }

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

    private static bool HasDebugSession(
        DebugSession debugSession,
        [NotNullWhen(true)] out Uri? debugSessionUri,
        [NotNullWhen(true)] out string? token,
        [NotNullWhen(true)] out byte[]? certData)
    {
        if (debugSession.Address is not null && debugSession.Token is not null && debugSession.ServerCertificate is not null && debugSession.TelemetryOptOut is not true)
        {
            debugSessionUri = new Uri($"https://{debugSession.Address}");
            token = debugSession.Token;
            certData = Convert.FromBase64String(debugSession.ServerCertificate);
            return true;
        }

        debugSessionUri = null;
        token = null;
        certData = null;
        return false;
    }

    private void StartSendLoop()
    {
        Debug.Assert(_httpClient is not null, "HttpClient must be initialized.");

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
                            await requestFunc(_httpClient, GetResponseProperty).ConfigureAwait(false);

                            // Double check properties are set.
                            foreach (var property in context.Properties)
                            {
                                if (!property.HasValue)
                                {
                                    _logger.LogWarning("Unset context property.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send telemetry request.");
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

    private HttpClient CreateHttpClient(Uri debugSessionUri, string token, byte[] certData)
    {
        var cert = new X509Certificate2(certData);
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, c, _, e) =>
            {
                // Server certificate is already considered valid.
                if (e == SslPolicyErrors.None)
                {
                    return true;
                }

                // Certificate isn't immediately valid. Check if it is the same as the one we expect.
                return string.Equals(
                    cert.GetCertHashString(HashAlgorithmName.SHA256),
                    c?.GetCertHashString(HashAlgorithmName.SHA256),
                    StringComparison.OrdinalIgnoreCase);
            }
        };
        var resolvedHandler = CreateHandler?.Invoke(handler) ?? handler;
        var client = new HttpClient(resolvedHandler)
        {
            BaseAddress = debugSessionUri,
            DefaultRequestHeaders =
            {
                { "Authorization", $"Bearer {token}" },
                { "User-Agent", "Aspire Dashboard" }
            }
        };

        return client;
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

    private async Task<bool> TryStartTelemetrySessionCoreAsync()
    {
        if (HasDebugSession(_options.Value.DebugSession, out var debugSessionUri, out var token, out var certData))
        {
            _httpClient = CreateHttpClient(debugSessionUri, token, certData);
        }
        else
        {
            _logger.LogInformation("Telemetry is not configured.");
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync(TelemetryEndpoints.TelemetryEnabled).ConfigureAwait(false);
            var isTelemetryEnabled = response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };

            if (!isTelemetryEnabled)
            {
                return false;
            }

            // start the actual telemetry session
            var telemetryStartedStatusCode = (await _httpClient.PostAsync(TelemetryEndpoints.TelemetryStart, content: null).ConfigureAwait(false)).StatusCode;
            return telemetryStartedStatusCode is HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            // If the request fails, we've been given an invalid server address and should assume telemetry is unsupported.
            _logger.LogWarning(ex, "Failed to request whether telemetry is supported.");
            return false;
        }
    }

    public void MakeRequest(OperationContext context, Func<HttpClient, Func<OperationContextProperty, object>, Task> requestFunc)
    {
        _channel.Writer.TryWrite((context, requestFunc));
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        if (_sendLoopTask is { } task)
        {
            await task.ConfigureAwait(false);
        }

        _httpClient?.Dispose();
    }
}
