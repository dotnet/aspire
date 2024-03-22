// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry.Instrumentation.StackExchangeRedis.Implementation;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using StackExchange.Redis.Profiling;

namespace OpenTelemetry.Instrumentation.StackExchangeRedis;

/// <summary>
/// StackExchange.Redis <see cref="IConnectionMultiplexer"/> instrumentation.
/// </summary>
internal sealed class StackExchangeRedisConnectionInstrumentation : IDisposable
{
    internal const string RedisDatabaseIndexKeyName = "db.redis.database_index";
    internal const string RedisFlagsKeyName = "db.redis.flags";
    internal static readonly string ActivitySourceName = typeof(StackExchangeRedisConnectionInstrumentation).Assembly.GetName().Name!;
    internal static readonly string ActivityName = ActivitySourceName + ".Execute";
    internal static readonly Version Version = typeof(StackExchangeRedisConnectionInstrumentation).Assembly.GetName().Version!;
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName, Version.ToString());
    internal static readonly IEnumerable<KeyValuePair<string, object?>> CreationTags = new[]
    {
        new KeyValuePair<string, object?>(SemanticConventions.AttributeDbSystem, "redis"),
    };

    internal readonly ConcurrentDictionary<(ActivityTraceId TraceId, ActivitySpanId SpanId), (Activity Activity, ProfilingSession Session)> Cache
        = new();

    private readonly StackExchangeRedisInstrumentationOptions options;
    private readonly EventWaitHandle stopHandle = new(false, EventResetMode.ManualReset);
    private readonly Thread drainThread;

    private readonly ProfilingSession defaultSession = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StackExchangeRedisConnectionInstrumentation"/> class.
    /// </summary>
    /// <param name="connection"><see cref="IConnectionMultiplexer"/> to instrument.</param>
    /// <param name="name">Optional name for the connection.</param>
    /// <param name="options">Configuration options for redis instrumentation.</param>
    public StackExchangeRedisConnectionInstrumentation(
        IConnectionMultiplexer connection,
        string? name,
        StackExchangeRedisInstrumentationOptions options)
    {
        Guard.ThrowIfNull(connection);

        this.options = options ?? new StackExchangeRedisInstrumentationOptions();

        this.drainThread = new Thread(this.DrainEntries)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "OpenTelemetry.Redis" : $"OpenTelemetry.Redis{{{name}}}",
            IsBackground = true,
        };
        this.drainThread.Start();

        connection.RegisterProfiler(this.GetProfilerSessionsFactory());
    }

    /// <summary>
    /// Returns session for the Redis calls recording.
    /// </summary>
    /// <returns>Session associated with the current span context to record Redis calls.</returns>
    public Func<ProfilingSession?> GetProfilerSessionsFactory()
    {
        return () =>
        {
            if (this.stopHandle.WaitOne(0))
            {
                return null;
            }

            var parent = Activity.Current;

            // If no parent use the default session.
            if (parent == null || parent.IdFormat != ActivityIdFormat.W3C)
            {
                return this.defaultSession;
            }

            // Try to reuse a session for all activities created under the same TraceId+SpanId.
            var cacheKey = (parent.TraceId, parent.SpanId);
            if (!this.Cache.TryGetValue(cacheKey, out var session))
            {
                session = (parent, new ProfilingSession());
                this.Cache.TryAdd(cacheKey, session);
            }

            return session.Session;
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.stopHandle.Set();
        this.drainThread.Join();

        this.Flush();

        this.stopHandle.Dispose();
    }

    internal void Flush()
    {
        RedisProfilerEntryToActivityConverter.DrainSession(null, this.defaultSession.FinishProfiling(), this.options);

        foreach (var entry in this.Cache)
        {
            var parent = entry.Value.Activity;
            if (parent.Duration == TimeSpan.Zero)
            {
                // Activity is still running, don't drain.
                continue;
            }

            ProfilingSession session = entry.Value.Session;
            RedisProfilerEntryToActivityConverter.DrainSession(parent, session.FinishProfiling(), this.options);
            this.Cache.TryRemove((entry.Key.TraceId, entry.Key.SpanId), out _);
        }
    }

    private void DrainEntries(object? state)
    {
        while (true)
        {
            if (this.stopHandle.WaitOne(this.options.FlushInterval))
            {
                break;
            }

            this.Flush();
        }
    }
}
