// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Utils;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public sealed class AspireTelemetryService : IAspireTelemetryService
{
    private readonly Lazy<HttpClient?> _httpClient;
    private bool? _telemetrySupported;
    private bool? _telemetryEnabled;
    private readonly IOptions<DashboardOptions> _options;
    private readonly ILocalStorage _localStorage;
    private readonly ITelemetrySender _telemetrySender;
    private readonly ILogger<AspireTelemetryService> _logger;

    public AspireTelemetryService(IOptions<DashboardOptions> options,
        ILocalStorage localStorage,
        AspireTelemetryService.ITelemetrySender telemetrySender,
        ILogger<AspireTelemetryService> logger)
    {
        _options = options;
        _localStorage = localStorage;
        _telemetrySender = telemetrySender;
        _logger = logger;
        _httpClient = new Lazy<HttpClient?>(() => CreateHttpClient(options.Value.DebugSession));
    }

    public bool IsTelemetryInitialized => _telemetrySupported is not null && _telemetryEnabled is not null;
    public bool IsTelemetrySupported => _telemetrySupported ?? throw new ArgumentNullException(nameof(_telemetrySupported), "InitializeAsync has not been called yet");
    public bool IsTelemetryEnabled => _telemetryEnabled ?? throw new ArgumentNullException(nameof(_telemetryEnabled), "InitializeAsync has not been called yet");

    public async Task InitializeAsync()
    {
        _logger.LogDebug("Initializing telemetry service.");

        if (_telemetrySupported is not null)
        {
            return;
        }

        _telemetrySupported = await GetTelemetrySupportedAsync(_httpClient.Value, _telemetrySender, _logger).ConfigureAwait(false);
        _telemetryEnabled = await GetTelemetryEnabledAsync(_telemetrySupported.Value, _options, _localStorage).ConfigureAwait(false);

        _logger.LogDebug("Initialized telemetry service. Telemetry enabled: {TelemetryEnabled}. Telemetry supported: {TelemetrySupported}", _telemetryEnabled, _telemetrySupported);

        // Post session property values after initialization, if telemetry has been enabled.
        if (_telemetryEnabled is true)
        {
            var propertyPostTasks = GetDefaultProperties().Select(defaultProperty => PostPropertyAsync(new PostPropertyRequest(defaultProperty.Key, defaultProperty.Value)));
            await Task.WhenAll(propertyPostTasks).ConfigureAwait(false);
        }

        return;

        static async Task<bool> GetTelemetryEnabledAsync(bool serverTelemetryEnabled, IOptions<DashboardOptions> options, ILocalStorage localStorage)
        {
            if (serverTelemetryEnabled is not true)
            {
                return false;
            }

            var storageResult = await localStorage.GetUnprotectedAsync<TelemetrySettings>(BrowserStorageKeys.DashboardTelemetrySettings).ConfigureAwait(false);
            if (storageResult.Value is { } telemetrySettings)
            {
                return telemetrySettings.IsEnabled;
            }

            return true;

        }

        static async Task<bool> GetTelemetrySupportedAsync(HttpClient? client, ITelemetrySender sender, ILogger<AspireTelemetryService> logger)
        {
            if (client is null)
            {
                return false;
            }

            try
            {
                var response = await sender.MakeRequestAsync(client, c => c.GetAsync(TelemetryEndpoints.TelemetryEnabled)).ConfigureAwait(false);
                var isTelemetryEnabled = response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };

                if (!isTelemetryEnabled)
                {
                    return false;
                }

                // start the actual telemetry session
                var telemetryStartedStatusCode = (await sender.MakeRequestAsync(client, c => c.PostAsync(TelemetryEndpoints.TelemetryStart, content: null)).ConfigureAwait(false)).StatusCode;
                return telemetryStartedStatusCode is HttpStatusCode.OK;
            }
            catch (HttpRequestException ex)
            {
                // If the request fails, we've been given an invalid server address and should assume telemetry is unsupported.
                logger.LogDebug("Failed to request whether telemetry is supported: {ExceptionMessage}", ex.Message);
                return false;
            }
        }
    }

    public async Task<bool> SetTelemetryEnabledAsync(bool enabled)
    {
        Debug.Assert(_telemetrySupported is not null);
        if (_telemetrySupported is false)
        {
            return false;
        }

        _telemetryEnabled = enabled;
        await _localStorage.SetUnprotectedAsync(BrowserStorageKeys.DashboardTelemetrySettings, new TelemetrySettings(IsEnabled: enabled)).ConfigureAwait(false);
        return true;
    }

    public async Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartOperation, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<StartOperationResponse>(response.StatusCode, await response.Content.ReadFromJsonAsync<StartOperationResponse>().ConfigureAwait(false)) :
            new TelemetryResponse<StartOperationResponse>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse?> EndOperationAsync(EndOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndOperation, request)).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartUserTask, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<StartOperationResponse>(response.StatusCode, await response.Content.ReadFromJsonAsync<StartOperationResponse>().ConfigureAwait(false)) :
            new TelemetryResponse<StartOperationResponse>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse?> EndUserTaskAsync(EndOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndUserTask, request)).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task PerformUserTaskAsync(StartOperationRequest request, Func<Task<OperationResult>> func)
    {
        await PerformOperationAsync(isUserTask: true, request, func).ConfigureAwait(false);
    }

    public async Task PerformOperationAsync(StartOperationRequest request, Func<Task<OperationResult>> func)
    {
        await PerformOperationAsync(isUserTask: false, request, func).ConfigureAwait(false);
    }

    public async Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostOperationAsync(PostOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostOperation, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, await response.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false)) :
            new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostUserTaskAsync(PostOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostUserTask, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, await response.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false)) :
            new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostFaultAsync(PostFaultRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostFault, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, await response.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false)) :
            new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostAssetAsync(PostAssetRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostAsset, request)).ConfigureAwait(false);
        return response.IsSuccessStatusCode
            ? new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, await response.Content.ReadFromJsonAsync<TelemetryEventCorrelation>().ConfigureAwait(false)) :
            new TelemetryResponse<TelemetryEventCorrelation>(response.StatusCode, null);
    }

    public async Task<ITelemetryResponse?> PostPropertyAsync(PostPropertyRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostProperty, request)).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostRecurringProperty, request)).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await _telemetrySender.MakeRequestAsync(client, c => c.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostCommandLineFlags, request)).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties()
    {
        return new Dictionary<string, AspireTelemetryProperty>
        {
            { TelemetryPropertyKeys.DashboardVersion, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty) },
            { TelemetryPropertyKeys.DashboardBuildId, new AspireTelemetryProperty(typeof(DashboardWebApplication).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty) },
        };
    }

    private async Task PerformOperationAsync(bool isUserTask, StartOperationRequest request, Func<Task<OperationResult>> func)
    {
        var startOperationTask = Task.Run(() => isUserTask ? StartUserTaskAsync(request) : StartOperationAsync(request));
        var operationResult = await func().ConfigureAwait(false);

        _ = Task.Run(async () =>
        {
            var operationId = (await startOperationTask.ConfigureAwait(false))?.Content?.OperationId;
            if (operationId is null)
            {
                return;
            }

            await EndOperationAsync(new EndOperationRequest(operationId, operationResult.Result, operationResult.ErrorMessage)).ConfigureAwait(false);
        });
    }

    private async Task<HttpClient?> GetHttpClientAsync()
    {
        await this.InitializeAsync().ConfigureAwait(false);
        if (_httpClient.Value is null || _telemetrySupported is false || _telemetryEnabled is false)
        {
            return null;
        }

        return _httpClient.Value;
    }

    private static HttpClient? CreateHttpClient(DebugSession debugSession)
    {
        if (!HasDebugSession(debugSession, out var debugSessionUri, out var token, out var certData))
        {
            return null;
        }

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

        static bool HasDebugSession(
            DebugSession debugSession,
            [NotNullWhen(true)] out Uri? debugSessionUri,
            [NotNullWhen(true)] out string? token,
            [NotNullWhen(true)] out byte[]? certData)
        {
            if (debugSession.Address is not null && debugSession.Token is not null && debugSession.ServerCertificate is not null && debugSession.TelemetryEnabled is not false)
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
    }

    private static class TelemetryEndpoints
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

    public record TelemetrySettings(bool IsEnabled);

    public interface ITelemetrySender
    {
        Task<HttpResponseMessage> MakeRequestAsync(HttpClient client, Func<HttpClient, Task<HttpResponseMessage>> requestFunc);
    }

    public class TelemetrySender : ITelemetrySender
    {
        public Task<HttpResponseMessage> MakeRequestAsync(HttpClient client, Func<HttpClient, Task<HttpResponseMessage>> requestFunc)
        {
            return requestFunc(client);
        }
    }
}
