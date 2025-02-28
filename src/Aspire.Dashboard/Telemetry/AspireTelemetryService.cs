// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Telemetry;

public sealed class AspireTelemetryService(IOptions<DashboardOptions> options) : IAspireTelemetryService
{
    private readonly Lazy<HttpClient?> _httpClient = new(() => CreateHttpClient(options.Value.DebugSession));
    private bool? _telemetryEnabled;

    public async Task<bool> IsTelemetryEnabledAsync()
    {
        _telemetryEnabled ??= await GetTelemetryEnabledAsync(_httpClient.Value).ConfigureAwait(false);
        return _telemetryEnabled.Value;

        static async Task<bool> GetTelemetryEnabledAsync(HttpClient? client)
        {
            if (client is null)
            {
                return false;
            }

            var response = await client.GetAsync(TelemetryEndpoints.TelemetryEnabled).ConfigureAwait(false);
            return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };
        }
    }

    public async Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("/telemetry/startOperation", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/endOperation", request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<TelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("/telemetry/startUserTask", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/endUserTask", request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostOperationAsync(PostOperationRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("/telemetry/operation", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/userTask", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/fault", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/asset", request).ConfigureAwait(false);
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

        var response = await client.PostAsJsonAsync("/telemetry/property", request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("/telemetry/recurringProperty", request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    public async Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request)
    {
        if (await GetHttpClientAsync().ConfigureAwait(false) is not { } client)
        {
            return null;
        }

        var response = await client.PostAsJsonAsync("/telemetry/commandLineFlags", request).ConfigureAwait(false);
        return new TelemetryResponse(response.StatusCode);
    }

    private async Task<HttpClient?> GetHttpClientAsync()
    {
        if (_httpClient.Value is null || await IsTelemetryEnabledAsync().ConfigureAwait(false) is not true)
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
}
