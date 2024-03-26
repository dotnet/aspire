// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable disable

using System.Diagnostics;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Instrumentation;

internal sealed class DiagnosticSourceListener : IObserver<KeyValuePair<string, object>>
{
    private readonly ListenerHandler handler;

    private readonly Action<string, string, Exception> logUnknownException;

    public DiagnosticSourceListener(ListenerHandler handler, Action<string, string, Exception> logUnknownException)
    {
        Guard.ThrowIfNull(handler);

        this.handler = handler;
        this.logUnknownException = logUnknownException;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(KeyValuePair<string, object> value)
    {
        if (!this.handler.SupportsNullActivity && Activity.Current == null)
        {
            return;
        }

        try
        {
            this.handler.OnEventWritten(value.Key, value.Value);
        }
        catch (Exception ex)
        {
            this.logUnknownException?.Invoke(this.handler?.SourceName, value.Key, ex);
        }
    }
}
