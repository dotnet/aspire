// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable disable

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using OpenTelemetry.Instrumentation.SqlClient.Implementation;
#endif

namespace OpenTelemetry.Instrumentation.SqlClient;

/// <summary>
/// SqlClient instrumentation.
/// </summary>
internal sealed class SqlClientInstrumentation : IDisposable
{
    internal const string SqlClientDiagnosticListenerName = "SqlClientDiagnosticListener";
#if NET6_0_OR_GREATER
    internal const string SqlClientTrimmingUnsupportedMessage = "Trimming is not yet supported with SqlClient instrumentation.";
#endif
#if NETFRAMEWORK
    private readonly SqlEventSourceListener sqlEventSourceListener;
#else
    private static readonly HashSet<string> DiagnosticSourceEvents = new()
    {
        "System.Data.SqlClient.WriteCommandBefore",
        "Microsoft.Data.SqlClient.WriteCommandBefore",
        "System.Data.SqlClient.WriteCommandAfter",
        "Microsoft.Data.SqlClient.WriteCommandAfter",
        "System.Data.SqlClient.WriteCommandError",
        "Microsoft.Data.SqlClient.WriteCommandError",
    };

    private readonly Func<string, object, object, bool> isEnabled = (eventName, _, _)
        => DiagnosticSourceEvents.Contains(eventName);

    private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlClientInstrumentation"/> class.
    /// </summary>
    /// <param name="options">Configuration options for sql instrumentation.</param>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(SqlClientTrimmingUnsupportedMessage)]
#endif
    public SqlClientInstrumentation(
        SqlClientTraceInstrumentationOptions options = null)
    {
#if NETFRAMEWORK
        this.sqlEventSourceListener = new SqlEventSourceListener(options);
#else
        this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(
           name => new SqlClientDiagnosticListener(name, options),
           listener => listener.Name == SqlClientDiagnosticListenerName,
           this.isEnabled,
           SqlClientInstrumentationEventSource.Log.UnknownErrorProcessingEvent);
        this.diagnosticSourceSubscriber.Subscribe();
#endif
    }

    /// <inheritdoc/>
    public void Dispose()
    {
#if NETFRAMEWORK
        this.sqlEventSourceListener?.Dispose();
#else
        this.diagnosticSourceSubscriber?.Dispose();
#endif
    }
}
