// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Telemetry;

namespace Aspire.Dashboard.Components.Tests.Shared;

public sealed class TestTelemetryErrorRecorder : ITelemetryErrorRecorder
{
    public void RecordError(string message, Exception exception, bool writeToLogging = false)
    {
    }
}
