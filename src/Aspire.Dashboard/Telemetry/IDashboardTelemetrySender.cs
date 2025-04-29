// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public interface IDashboardTelemetrySender : IAsyncDisposable
{
    public Task<bool> TryStartTelemetrySessionAsync();

    public TelemetrySessionState State { get; }

    public void QueueRequest(OperationContext context, Func<HttpClient, Func<OperationContextProperty, object>, Task> requestFunc);
}

public enum TelemetrySessionState
{
    Uninitialized,
    Enabled,
    Disabled
}
