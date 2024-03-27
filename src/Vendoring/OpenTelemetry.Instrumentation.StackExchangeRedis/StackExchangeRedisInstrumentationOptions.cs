// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Trace;
using StackExchange.Redis.Profiling;

namespace OpenTelemetry.Instrumentation.StackExchangeRedis;

/// <summary>
/// Options for StackExchange.Redis instrumentation.
/// </summary>
internal class StackExchangeRedisInstrumentationOptions
{
    /// <summary>
    /// Gets or sets the maximum time that should elapse between flushing the internal buffer of Redis profiling sessions and creating <see cref="Activity"/> objects. Default value: 00:00:10.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see cref="StackExchangeRedisConnectionInstrumentation"/> should use reflection to get more detailed <see cref="SemanticConventions.AttributeDbStatement"/> tag values. Default value: False.
    /// </summary>
    public bool SetVerboseDatabaseStatements { get; set; }

    /// <summary>
    /// Gets or sets an action to enrich an Activity.
    /// </summary>
    /// <remarks>
    /// <para><see cref="Activity"/>: the activity being enriched.</para>
    /// <para><see cref="IProfiledCommand"/>: the profiled redis command from which additional information can be extracted to enrich the activity.</para>
    /// </remarks>
    public Action<Activity, IProfiledCommand>? Enrich { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether or not the <see cref="StackExchangeRedisConnectionInstrumentation"/> should enrich Activity with <see cref="ActivityEvent"/> entries about the Redis command processing/lifetime. Defaults to <see cref="bool">true</see>.
    /// </summary>
    public bool EnrichActivityWithTimingEvents { get; set; } = true;
}
