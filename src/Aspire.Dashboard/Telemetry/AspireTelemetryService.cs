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

public sealed class AspireTelemetryService(IOptions<DashboardOptions> options, ILocalStorage localStorage) : IAspireTelemetryService
{
    private readonly Lazy<HttpClient?> _httpClient = new(() => CreateHttpClient(options.Value.DebugSession));
    private bool? _serverTelemetryEnabled;
    private bool? _telemetryEnabled;

    public bool IsTelemetrySupported => _serverTelemetryEnabled ?? throw new ArgumentNullException(nameof(_serverTelemetryEnabled), "InitializeAsync has not been called yet");
    public bool IsTelemetryEnabled => _telemetryEnabled ?? throw new ArgumentNullException(nameof(_telemetryEnabled), "InitializeAsync has not been called yet");

    public async Task InitializeAsync()
    {
        _serverTelemetryEnabled ??= await GetTelemetrySupportedAsync(_httpClient.Value).ConfigureAwait(false);

        if (_telemetryEnabled is null)
        {
            _telemetryEnabled = await GetTelemetryEnabledAsync(_serverTelemetryEnabled, localStorage).ConfigureAwait(false);

            // Post session property values after initialization, if telemetry has been enabled.
            if (_telemetryEnabled is true)
            {
                var propertyPostTasks = GetDefaultProperties().Select(defaultProperty => PostPropertyAsync(new PostPropertyRequest(defaultProperty.Key, defaultProperty.Value)));
                await Task.WhenAll(propertyPostTasks).ConfigureAwait(false);
            }
        }

        return;

        static async Task<bool> GetTelemetryEnabledAsync(bool? serverTelemetryEnabled, ILocalStorage localStorage)
        {
            if (serverTelemetryEnabled is true)
            {
                var storageResult = await localStorage.GetUnprotectedAsync<TelemetrySettings>(BrowserStorageKeys.DashboardTelemetrySettings).ConfigureAwait(false);
                if (storageResult.Value is { } telemetrySettings)
                {
                    return telemetrySettings.IsEnabled;
                }

                return true;
            }

            return false;
        }

        static async Task<bool> GetTelemetrySupportedAsync(HttpClient? client)
        {
            if (client is null)
            {
                return false;
            }

            var response = await client.GetAsync(TelemetryEndpoints.TelemetryEnabled).ConfigureAwait(false);
            var isTelemetryEnabled = response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };

            if (!isTelemetryEnabled)
            {
                return false;
            }

            // start the actual telemetry session
            var telemetryStartedStatusCode = (await client.PostAsync(TelemetryEndpoints.TelemetryStart, content: null).ConfigureAwait(false)).StatusCode;
            return telemetryStartedStatusCode is HttpStatusCode.OK;
        }
    }

    public async Task<bool> SetTelemetryEnabledAsync(bool enabled)
    {
        Debug.Assert(_serverTelemetryEnabled is not null);
        if (_serverTelemetryEnabled is false)
        {
            return false;
        }

        _telemetryEnabled = enabled;
        await localStorage.SetUnprotectedAsync(BrowserStorageKeys.DashboardTelemetrySettings, new TelemetrySettings(IsEnabled: enabled)).ConfigureAwait(false);
        return true;
    }

    public async Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartOperation, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndOperation, request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryStartUserTask, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryEndUserTask, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostOperation, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostUserTask, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostFault, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostAsset, request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostProperty, request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostRecurringProperty, request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync(TelemetryEndpoints.TelemetryPostCommandLineFlags, request).ConfigureAwait(false);
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
        if (_httpClient.Value is null || _serverTelemetryEnabled is false || _telemetryEnabled is false)
        {
            return null;
        }

        return _httpClient.Value;
    }

    private static HttpClient? CreateHttpClient(DebugSession debugSession)
    {
        if (!SupportsTelemetry(debugSession, out var debugSessionUri, out var token, out var certData))
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

        static bool SupportsTelemetry(DebugSession debugSession, [NotNullWhen(true)] out Uri? debugSessionUri, [NotNullWhen(true)] out string? token, [NotNullWhen(true)] out byte[]? certData)
        {
            if (debugSession.Address is not null && debugSession.Token is not null && debugSession.ServerCertificate is not null)
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

    private record TelemetrySettings(bool IsEnabled);
}
