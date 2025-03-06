// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Channels;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public sealed class DashboardTelemetryService : IDashboardTelemetryService
{
    private bool? _telemetryEnabled;
    private IDashboardTelemetrySender? _dashboardTelemetrySender;
    private readonly IOptions<DashboardOptions> _options;
    private readonly ILogger<DashboardTelemetryService> _logger;

    public DashboardTelemetryService(
        IOptions<DashboardOptions> options,
        ILogger<DashboardTelemetryService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public bool IsTelemetryInitialized => _telemetryEnabled is not null;
    public bool IsTelemetryEnabled => _telemetryEnabled ?? throw new ArgumentNullException(nameof(_telemetryEnabled), "InitializeAsync has not been called yet");

    public async Task InitializeAsync(IDashboardTelemetrySender? telemetrySender = null)
    {
        _logger.LogDebug("Initializing telemetry service.");

        if (_telemetryEnabled is not null)
        {
            return;
        }

        if (telemetrySender is not null)
        {
            _dashboardTelemetrySender = telemetrySender;
        }
        else if (!HasDebugSession(_options.Value.DebugSession, out var debugSessionUri, out var token, out var certData))
        {
            _telemetryEnabled = false;
            _logger.LogDebug("Initialized telemetry service. Telemetry enabled: {TelemetryEnabled}", false);
            return;
        }
        else
        {
            _dashboardTelemetrySender = new DashboardTelemetrySender(CreateHttpClient(debugSessionUri, token, certData), _logger);
        }

        _telemetryEnabled = await GetTelemetrySupportedAsync(_dashboardTelemetrySender, _logger).ConfigureAwait(false);
        _logger.LogDebug("Initialized telemetry service. Telemetry enabled: {TelemetryEnabled}", _telemetryEnabled);

        // Post session property values after initialization, if telemetry has been enabled.
        if (_telemetryEnabled is true)
        {
            foreach (var (key, value) in GetDefaultProperties())
            {
                PostProperty(key, value);
            }
        }

        return;

        static HttpClient CreateHttpClient(Uri debugSessionUri, string token, byte[] certData)
        {
            var cert = new X509Certificate2(certData);
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, c, _, _) => cert.Equals(c)
            };
            var client = new HttpClient(handler)
            {
                BaseAddress = debugSessionUri,
                DefaultRequestHeaders = { { "Authorization", $"Bearer {token}" }, { "User-Agent", "Aspire Dashboard" } }
            };

            return client;
        }

        static bool HasDebugSession(
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

        static async Task<bool> GetTelemetrySupportedAsync(IDashboardTelemetrySender sender, ILogger<DashboardTelemetryService> logger)
        {
            try
            {
                var response = await sender.GetTelemetryEnabledAsync().ConfigureAwait(false);
                var isTelemetryEnabled = response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };

                if (!isTelemetryEnabled)
                {
                    return false;
                }

                // start the actual telemetry session
                var telemetryStartedStatusCode = (await sender.StartTelemetrySessionAsync().ConfigureAwait(false)).StatusCode;
                return telemetryStartedStatusCode is HttpStatusCode.OK;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                // If the request fails, we've been given an invalid server address and should assume telemetry is unsupported.
                logger.LogDebug("Failed to request whether telemetry is supported: {ExceptionMessage}", ex.Message);
                return false;
            }
        }
    }

    public (Guid OperationIdToken, Guid CorrelationToken) StartOperation(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        var guids = _dashboardTelemetrySender.MakeRequest(2, async (client, propertyGetter) =>
        {
            var scopeSettings = new AspireTelemetryScopeSettings(
                startEventProperties,
                severity,
                isOptOutFriendly,
                correlations?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray(),
                postStartEvent);

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartOperation, scopeSettings).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<StartOperationResponse>().ConfigureAwait(false);
            Debug.Assert(response is not null);

            return [response.OperationId, response.Correlation];
        });

        return (guids[0], guids[1]);
    }

    public void EndOperation(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, propertyGetter) =>
        {
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndOperation, new EndOperationRequest(Id: (string)propertyGetter(operationId), Result: result, ErrorMessage: errorMessage)).ConfigureAwait(false);
            return [];
        });
    }

    public (Guid OperationIdToken, Guid CorrelationToken) StartUserTask(string eventName, Dictionary<string, AspireTelemetryProperty> startEventProperties, TelemetrySeverity severity = TelemetrySeverity.Normal, bool isOptOutFriendly = false, bool postStartEvent = true, IEnumerable<Guid>? correlations = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        var guids = _dashboardTelemetrySender.MakeRequest(2, async (client, propertyGetter) =>
        {
            var scopeSettings = new AspireTelemetryScopeSettings(
                startEventProperties,
                severity,
                isOptOutFriendly,
                correlations?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray(),
                postStartEvent);

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartUserTask, scopeSettings).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<StartOperationResponse>().ConfigureAwait(false);
            Debug.Assert(response is not null);

            return [response.OperationId, response.Correlation];
        });

        return (guids[0], guids[1]);
    }

    public void EndUserTask(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, propertyGetter) =>
        {
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndUserTask, new EndOperationRequest(Id: (string)propertyGetter(operationId), Result: result, ErrorMessage: errorMessage)).ConfigureAwait(false);
            return [];
        });
    }

    public Guid PostOperation(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        return _dashboardTelemetrySender.MakeRequest(1, async (client, propertyGetter) =>
        {
            var request = new PostOperationRequest(
                eventName,
                result,
                resultSummary,
                properties,
                correlatedWith?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray());

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostOperation, request).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false);
            Debug.Assert(response is not null);
            return [response];
        }).Single();
    }

    public Guid PostUserTask(string eventName, TelemetryResult result, string? resultSummary = null, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        return _dashboardTelemetrySender.MakeRequest(1, async (client, propertyGetter) =>
        {
            var request = new PostOperationRequest(
                eventName,
                result,
                resultSummary,
                properties,
                correlatedWith?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray());

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostUserTask, request).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false);
            Debug.Assert(response is not null);
            return [response];
        }).Single();
    }

    public Guid PostFault(string eventName, string description, FaultSeverity severity, Dictionary<string, AspireTelemetryProperty>? properties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        return _dashboardTelemetrySender.MakeRequest(1, async (client, propertyGetter) =>
        {
            var request = new PostFaultRequest(
                eventName,
                description,
                severity,
                properties,
                correlatedWith?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray());

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostFault, request).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false);
            Debug.Assert(response is not null);
            return [response];
        }).Single();
    }

    public Guid PostAsset(string eventName, string assetId, int assetEventVersion, Dictionary<string, AspireTelemetryProperty>? additionalProperties = null, IEnumerable<Guid>? correlatedWith = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        return _dashboardTelemetrySender.MakeRequest(1, async (client, propertyGetter) =>
        {
            var request = new PostAssetRequest(
                eventName,
                assetId,
                assetEventVersion,
                additionalProperties,
                correlatedWith?.Select(propertyGetter).Cast<TelemetryEventCorrelation>().ToArray());

            var httpResponseMessage = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostAsset, request).ConfigureAwait(false);
            httpResponseMessage.EnsureSuccessStatusCode();
            var response = await httpResponseMessage.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false);
            Debug.Assert(response is not null);
            return [response];
        }).Single();
    }

    public void PostProperty(string propertyName, AspireTelemetryProperty propertyValue)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, _) =>
        {
            var request = new PostPropertyRequest(propertyName, propertyValue);
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostProperty, request).ConfigureAwait(false);
            return [];
        });
    }

    public void PostRecurringProperty(string propertyName, AspireTelemetryProperty propertyValue)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, _) =>
        {
            var request = new PostPropertyRequest(propertyName, propertyValue);
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostRecurringProperty, request).ConfigureAwait(false);
            return [];
        });
    }

    public void PostCommandLineFlags(List<string> flagPrefixes, Dictionary<string, AspireTelemetryProperty> additionalProperties)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, _) =>
        {
            var request = new PostCommandLineFlagsRequest(flagPrefixes, additionalProperties);
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostCommandLineFlags, request).ConfigureAwait(false);
            return [];
        });
    }

    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties()
    {
        return new Dictionary<string, AspireTelemetryProperty>
        {
            { TelemetryPropertyKeys.DashboardVersion, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty) },
            { TelemetryPropertyKeys.DashboardBuildId, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty) },
        };
    }
}

public static class TelemetryEndpoints
{
    public const string TelemetryEnabled = "/telemetry/enabled";
    public const string TelemetryStart = "/telemetry/start";
    public const string TelemetryStartOperation = "/telemetry/startOperation";
    public const string TelemetryEndOperation = "/telemetry/endOperation";
    public const string TelemetryStartUserTask = "/telemetry/startUserTask";
    public const string TelemetryEndUserTask = "/telemetry/endUserTask";
    public const string TelemetryPostOperation = "/telemetry/operation";
    public const string TelemetryPostUserTask = "/telemetry/userTask";
    public const string TelemetryPostFault = "/telemetry/fault";
    public const string TelemetryPostAsset = "/telemetry/asset";
    public const string TelemetryPostProperty = "/telemetry/property";
    public const string TelemetryPostRecurringProperty = "/telemetry/recurringProperty";
    public const string TelemetryPostCommandLineFlags = "/telemetry/commandLineFlags";
}

public class DashboardTelemetrySender : IDashboardTelemetrySender
{
    private readonly HttpClient _httpClient;

    private readonly Channel<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)> _channel = Channel.CreateUnbounded<(List<Guid>, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>>)>();
    private readonly ConcurrentDictionary<Guid, object> _responsePropertyMap = [];

    public DashboardTelemetrySender(HttpClient client, ILogger<IDashboardTelemetryService> logger)
    {
        _httpClient = client;
        _ = Task.Run(async () =>
        {
            while (await _channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (_channel.Reader.TryRead(out var operation))
                {
                    var (guids, requestFunc) = operation;
                    try
                    {
                        var result = await requestFunc(client, GetResponseProperty).ConfigureAwait(false);
                        foreach (var (guid, value) in guids.Zip(result))
                        {
                            _responsePropertyMap[guid] = value;
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException or JsonException or ArgumentException)
                    {
                        logger.LogWarning("Failed to make telemetry request: {ExceptionMessage}", ex.Message);
                    }

                    continue;

                    object GetResponseProperty(Guid guid)
                    {
                        if (!_responsePropertyMap.TryGetValue(guid, out var value))
                        {
                            throw new ArgumentException("Response property not found, maybe a dependent telemetry request failed?", nameof(guid));
                        }

                        return value;
                    }
                }
            }
        });
    }

    public Task<HttpResponseMessage> GetTelemetryEnabledAsync()
    {
        return _httpClient.GetAsync(TelemetryEndpoints.TelemetryEnabled);
    }

    public Task<HttpResponseMessage> StartTelemetrySessionAsync()
    {
        return _httpClient.PostAsync(TelemetryEndpoints.TelemetryStart, content: null);
    }

    public List<Guid> MakeRequest(int generatedGuids, Func<HttpClient, Func<Guid, object>, Task<ICollection<object>>> requestFunc)
    {
        Debug.Assert(generatedGuids >= 0, "guidsNeeded must be >= 0");

        var guids = new List<Guid>();
        for (var i = 0; i < generatedGuids; i++)
        {
            guids.Add(Guid.NewGuid());
        }

        _channel.Writer.TryWrite((guids, requestFunc));

        return guids;
    }
}
