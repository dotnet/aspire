// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public sealed class DashboardTelemetryService(IOptions<DashboardOptions> options, ILogger<DashboardTelemetryService> logger) : IDisposable
{
    private bool? _telemetryEnabled;
    private IDashboardTelemetrySender? _dashboardTelemetrySender;

    /// <summary>
    /// Whether the telemetry service has been initialized. This will be true if <see cref="DashboardTelemetryService.InitializeAsync"/> has completed.
    /// </summary>
    public bool IsTelemetryInitialized => _telemetryEnabled is not null;

    /// <summary>
    /// Whether telemetry is enabled in the current environment. This will be false if:
    /// <list type="bullet">
    /// <item>The user is not running the Aspire dashboard through a supported IDE version</item>
    /// <item>The dashboard resource contains a telemetry opt-out config entry</item>
    /// <item>The IDE instance has opted out of telemetry</item>
    /// </list>
    /// </summary>
    public bool IsTelemetryEnabled => _telemetryEnabled ?? throw new ArgumentNullException(nameof(_telemetryEnabled), "InitializeAsync has not been called yet");

    /// <summary>
    /// Call before using any telemetry methods. This will initialize the telemetry service and ensure that <see cref="DashboardTelemetryService.IsTelemetryEnabled"/> is set
    /// by making a request to the debug session, if one exists.
    /// </summary>
    public async Task InitializeAsync(IDashboardTelemetrySender? telemetrySender = null)
    {
        logger.LogDebug("Initializing telemetry service.");

        if (_telemetryEnabled is not null)
        {
            return;
        }

        if (telemetrySender is not null)
        {
            _dashboardTelemetrySender = telemetrySender;
        }
        else if (!HasDebugSession(options.Value.DebugSession, out var debugSessionUri, out var token, out var certData))
        {
            _telemetryEnabled = false;
            logger.LogDebug("Initialized telemetry service. Telemetry enabled: {TelemetryEnabled}", false);
            return;
        }
        else
        {
            _dashboardTelemetrySender = new DashboardTelemetrySender(CreateHttpClient(debugSessionUri, token, certData), logger);
        }

        _telemetryEnabled = await GetTelemetrySupportedAsync(_dashboardTelemetrySender, logger).ConfigureAwait(false);
        logger.LogDebug("Initialized telemetry service. Telemetry enabled: {TelemetryEnabled}", _telemetryEnabled);

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

    /// <summary>
    /// Begin a long-running user operation. Prefer this over <see cref="DashboardTelemetryService.PostOperation"/>. If an explicit user task caused this operation to start,
    /// use <see cref="DashboardTelemetryService.StartUserTask"/> instead. Duration will be automatically calculated and the end event posted after <see cref="DashboardTelemetryService.EndOperation"/> is called.
    /// </summary>
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

    /// <summary>
    /// Ends a long-running operation. This will post the end event and calculate the duration.
    /// </summary>
    public void EndOperation(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, propertyGetter) =>
        {
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndOperation, new EndOperationRequest(Id: (string)propertyGetter(operationId), Result: result, ErrorMessage: errorMessage)).ConfigureAwait(false);
            return [];
        });
    }

    /// <summary>
    /// Begin a long-running user task. This will post the start event and calculate the duration.
    /// Duration will be automatically calculated and the end event posted after <see cref="DashboardTelemetryService.EndUserTask"/> is called.
    /// </summary>
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

    /// <summary>
    /// Ends a long-running user task. This will post the end event and calculate the duration.
    /// </summary>
    public void EndUserTask(Guid operationId, TelemetryResult result, string? errorMessage = null)
    {
        Debug.Assert(_dashboardTelemetrySender is not null, "Telemetry sender is not initialized");
        _dashboardTelemetrySender.MakeRequest(0, async (client, propertyGetter) =>
        {
            await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndUserTask, new EndOperationRequest(Id: (string)propertyGetter(operationId), Result: result, ErrorMessage: errorMessage)).ConfigureAwait(false);
            return [];
        });
    }

    /// <summary>
    /// Posts a short-lived operation. If duration needs to be calculated, use <see cref="DashboardTelemetryService.StartOperation"/> and <see cref="DashboardTelemetryService.EndOperation"/> instead.
    /// If an explicit user task caused this operation to start, use <see cref="DashboardTelemetryService.PostUserTask"/> instead.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
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

    /// <summary>
    /// Posts a short-lived user task. If duration needs to be calculated, use <see cref="DashboardTelemetryService.StartUserTask"/> and <see cref="DashboardTelemetryService.EndUserTask"/> instead.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
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

    /// <summary>
    /// Posts a fault event.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
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

    /// <summary>
    /// Posts an asset event. This is used to track events that are related to a specific asset, whose correlations can be sent along with other events.
    /// Currently not used.
    /// <returns>Guid corresponding to the (as-of-yet-uncompleted) correlation returned from this request.</returns>
    /// </summary>
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

    /// <summary>
    /// Post a session property.
    /// </summary>
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

    /// <summary>
    /// Post a session recurring property.
    /// </summary>
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

    /// <summary>
    /// Currently not used.
    /// </summary>
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

    /// <summary>
    /// Gets identifying properties for the telemetry session.
    /// </summary>
    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties()
    {
        return new Dictionary<string, AspireTelemetryProperty>
        {
            { TelemetryPropertyKeys.DashboardVersion, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty) },
            { TelemetryPropertyKeys.DashboardBuildId, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty) },
        };
    }

    public void Dispose()
    {
        _dashboardTelemetrySender?.Dispose();
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
