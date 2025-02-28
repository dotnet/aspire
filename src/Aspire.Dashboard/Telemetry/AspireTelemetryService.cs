// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Aspire.Dashboard.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Telemetry;

namespace Aspire.Dashboard.Telemetry;

public sealed class AspireTelemetryService(IOptions<DashboardOptions> options)
{
    private readonly Lazy<HttpClient?> _httpClient = new(() => CreateHttpClient(options.Value.DebugSession));
    private bool? _telemetryEnabled;

    public async Task<bool> IsTelemetryEnabledAsync()
    {
        _telemetryEnabled ??= await GetTelemetryEnabledAsync().ConfigureAwait(false);
        return _telemetryEnabled.Value;
    }

    private async Task<bool> GetTelemetryEnabledAsync()
    {
        var client = _httpClient.Value;
        if (client is null)
        {
            return false;
        }

        var response = await client.GetAsync(TelemetryEndpoints.TelemetryEnabled).ConfigureAwait(false);
        return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<TelemetryEnabledResponse>().ConfigureAwait(false) is { IsEnabled: true };
    }

    private static HttpClient? CreateHttpClient(DebugSession debugSession)
    {
        if (!SupportsTelemetry(debugSession, out var debugSessionUri, out var token))
        {
            return null;
        }

        var client = new HttpClient
        {
            BaseAddress = debugSessionUri,
            DefaultRequestHeaders = { { "Authorization", $"Bearer {token}" } }
        };

        client.DefaultRequestHeaders.Add("User-Agent", "Aspire Dashboard");
        return client;

        static bool SupportsTelemetry(DebugSession debugSession, [NotNullWhen(true)] out Uri? debugSessionUri, out string? token)
        {
            if (debugSession.Address is not null && debugSession.Token is not null)
            {
                debugSessionUri = new Uri(debugSession.Address);
                token = debugSession.Token;
                return true;
            }

            debugSessionUri = null;
            token = null;
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

    public record TelemetryEnabledResponse(bool IsEnabled);
    public record StartOperationRequest(string EventName, AspireTelemetryScopeSettings? Settings = null);
    public record StartOperationResponse(string OperationId);
    public record EndOperationRequest(string Id, TelemetryResult Result, string? ErrorMessage = null);
    public record PostOperationRequest(string EventName, TelemetryResult Result, string? ResultSummary = null, TelemetryEventCorrelation[]? CorrelatedWith = null);
    public record PostFaultRequest(string EventName, string Description, FaultSeverity Severity, TelemetryEventCorrelation[]? CorrelatedWith = null);
    public record PostAssetRequest(string EventName, string AssetId, int AssetEventVersion, Dictionary<string, object>? AdditionalProperties, TelemetryEventCorrelation[]? CorrelatedWith = null);
    public record PostPropertyRequest(string PropertyName, string PropertyValue);
    public record PostCommandLineFlagsRequest(List<string> FlagPrefixes, Dictionary<string, object> AdditionalProperties);

    public record AspireTelemetryScopeSettings(
        Dictionary<string, object> StartEventProperties,
        TelemetrySeverity Severity = TelemetrySeverity.Normal,
        bool IsOptOutFriendly = false,
        TelemetryEventCorrelation[]? Correlations = null,
        bool PostStartEvent = true
    );

    public class TelemetryEventCorrelation
    {
        [JsonPropertyName("id")]
        public required Guid Id { get; set; }

        [JsonPropertyName("eventType")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataModelEventType EventType { get; set; }
    }
}
