// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#nullable disable

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Instrumentation.SqlClient.Implementation;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Trace;

/// <summary>
/// Extension methods to simplify registering of dependency instrumentation.
/// </summary>
internal static class TracerProviderBuilderExtensions
{
    /// <summary>
    /// Enables SqlClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(SqlClientInstrumentation.SqlClientTrimmingUnsupportedMessage)]
#endif
    public static TracerProviderBuilder AddSqlClientInstrumentation(this TracerProviderBuilder builder)
        => AddSqlClientInstrumentation(builder, name: null, configureSqlClientTraceInstrumentationOptions: null);

    /// <summary>
    /// Enables SqlClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <param name="configureSqlClientTraceInstrumentationOptions">Callback action for configuring <see cref="SqlClientTraceInstrumentationOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(SqlClientInstrumentation.SqlClientTrimmingUnsupportedMessage)]
#endif
    public static TracerProviderBuilder AddSqlClientInstrumentation(
        this TracerProviderBuilder builder,
        Action<SqlClientTraceInstrumentationOptions> configureSqlClientTraceInstrumentationOptions)
        => AddSqlClientInstrumentation(builder, name: null, configureSqlClientTraceInstrumentationOptions);

    /// <summary>
    /// Enables SqlClient instrumentation.
    /// </summary>
    /// <param name="builder"><see cref="TracerProviderBuilder"/> being configured.</param>
    /// <param name="name">Name which is used when retrieving options.</param>
    /// <param name="configureSqlClientTraceInstrumentationOptions">Callback action for configuring <see cref="SqlClientTraceInstrumentationOptions"/>.</param>
    /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode(SqlClientInstrumentation.SqlClientTrimmingUnsupportedMessage)]
#endif

    public static TracerProviderBuilder AddSqlClientInstrumentation(
        this TracerProviderBuilder builder,
        string name,
        Action<SqlClientTraceInstrumentationOptions> configureSqlClientTraceInstrumentationOptions)
    {
        Guard.ThrowIfNull(builder);

        name ??= Options.DefaultName;

        if (configureSqlClientTraceInstrumentationOptions != null)
        {
            builder.ConfigureServices(services => services.Configure(name, configureSqlClientTraceInstrumentationOptions));
        }

        builder.AddInstrumentation(sp =>
        {
            var sqlOptions = sp.GetRequiredService<IOptionsMonitor<SqlClientTraceInstrumentationOptions>>().Get(name);

            return new SqlClientInstrumentation(sqlOptions);
        });

        builder.AddSource(SqlActivitySourceHelper.ActivitySourceName);

        return builder;
    }
}
