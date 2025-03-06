// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Tests.Shared;

public class TestTelemetryService : IDashboardTelemetryService
{
    public Task InitializeAsync() => Task.CompletedTask;

    public bool IsTelemetryInitialized { get; } = true;
    public bool IsTelemetryEnabled { get; } = true;

    public Task<ITelemetryResponse<StartOperationResponse>?> StartOperationAsync(StartOperationRequest request) => Task.FromResult<ITelemetryResponse<StartOperationResponse>?>(null);

    public Task<ITelemetryResponse?> EndOperationAsync(EndOperationRequest request) => Task.FromResult<ITelemetryResponse?>(null);

    public Task<ITelemetryResponse<StartOperationResponse>?> StartUserTaskAsync(StartOperationRequest request) => Task.FromResult<ITelemetryResponse<StartOperationResponse>?>(null);

    public Task<ITelemetryResponse?> EndUserTaskAsync(EndOperationRequest request) => Task.FromResult<ITelemetryResponse?>(null);

    public Task PerformOperationAsync(StartOperationRequest request, Func<Task<OperationResult>> func) => Task.CompletedTask;

    public Task PerformUserTaskAsync(StartOperationRequest request, Func<Task<OperationResult>> func) => Task.CompletedTask;

    public Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostOperationAsync(PostOperationRequest request) => Task.FromResult<ITelemetryResponse<TelemetryEventCorrelation>?>(null);

    public Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostUserTaskAsync(PostOperationRequest request) => Task.FromResult<ITelemetryResponse<TelemetryEventCorrelation>?>(null);

    public Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostFaultAsync(PostFaultRequest request) => Task.FromResult<ITelemetryResponse<TelemetryEventCorrelation>?>(null);

    public Task<ITelemetryResponse<TelemetryEventCorrelation>?> PostAssetAsync(PostAssetRequest request) => Task.FromResult<ITelemetryResponse<TelemetryEventCorrelation>?>(null);

    public Task<ITelemetryResponse?> PostPropertyAsync(PostPropertyRequest request) => Task.FromResult<ITelemetryResponse?>(null);

    public Task<ITelemetryResponse?> PostRecurringPropertyAsync(PostPropertyRequest request) => Task.FromResult<ITelemetryResponse?>(null);

    public Task<ITelemetryResponse?> PostCommandLineFlagsAsync(PostCommandLineFlagsRequest request) => Task.FromResult<ITelemetryResponse?>(null);

    public Dictionary<string, AspireTelemetryProperty> GetDefaultProperties() => [];
}
