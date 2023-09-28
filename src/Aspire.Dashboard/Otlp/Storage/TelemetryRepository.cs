// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

namespace Aspire.Dashboard.Otlp.Storage;

public class TelemetryRepository
{
    private int MaxOperationCount { get; init; }
    private int MaxLogCount { get; init; }

    private readonly object _lock = new();
    private readonly ILogger _logger;

    private readonly List<Subscription> _applicationSubscriptions = new();
    private readonly List<Subscription> _logSubscriptions = new();
    private readonly List<Subscription> _metricsSubscriptions = new();
    private readonly List<Subscription> _tracingSubscriptions = new();

    private readonly ConcurrentDictionary<string, OtlpApplication> _applications = new();

    public TelemetryRepository(IConfiguration config, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(TelemetryRepository));
        MaxOperationCount = config.GetValue(nameof(MaxOperationCount), 128);
        MaxLogCount = config.GetValue(nameof(MaxLogCount), 4096);
    }

    public List<OtlpApplication> GetApplications()
    {
        var applications = new List<OtlpApplication>();
        foreach (var kvp in _applications)
        {
            applications.Add(kvp.Value);
        }
        applications.Sort((a, b) => string.Compare(a.ApplicationName, b.ApplicationName, StringComparison.OrdinalIgnoreCase));
        return applications;
    }

    public OtlpApplication GetOrAddApplication(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var serviceInstanceId = resource.GetServiceId();
        if (serviceInstanceId is null)
        {
            throw new InvalidOperationException($"Resource does not have a '{OtlpApplication.SERVICE_INSTANCE_ID}' attribute.");
        }

        // Fast path.
        if (_applications.TryGetValue(serviceInstanceId, out var application))
        {
            return application;
        }

        // Slower get or add path.
        (application, var isNew) = GetOrAddApplication(serviceInstanceId, resource);
        if (isNew)
        {
            RaiseSubscriptionChanged(_applicationSubscriptions);
        }

        return application;

        (OtlpApplication, bool) GetOrAddApplication(string serviceId, Resource resource)
        {
            // This GetOrAdd allocates a closure, so we avoid it if possible.
            var newApplication = false;
            var application = _applications.GetOrAdd(serviceId, _ =>
            {
                newApplication = true;
                return new OtlpApplication(resource, _applications, _logger);
            });
            return (application, newApplication);
        }
    }

    public Subscription OnNewApplications(Func<Task> callback)
    {
        return AddSubscription(string.Empty, callback, _applicationSubscriptions);
    }

    public Subscription OnNewLogs(string applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _logSubscriptions);
    }

    public Subscription OnNewMetrics(string applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _metricsSubscriptions);
    }

    public Subscription OnNewTracing(string applicationId, Func<Task> callback)
    {
        return AddSubscription(applicationId, callback, _tracingSubscriptions);
    }

    private Subscription AddSubscription(string applicationId, Func<Task> callback, List<Subscription> subscriptions)
    {
        Subscription? subscription = null;
        subscription = new Subscription(applicationId, callback, () =>
        {
            lock (_lock)
            {
                subscriptions.Remove(subscription!);
            }
        });

        lock (_lock)
        {
            subscriptions.Add(subscription);
        }

        return subscription;
    }

    private void RaiseSubscriptionChanged(List<Subscription> subscriptions)
    {
        lock (_lock)
        {
            foreach (var subscription in subscriptions)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await subscription.ExecuteAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in subscription callback");
                    }
                });
            }
        }
    }

    public void AddLogs(AddContext context, RepeatedField<ResourceLogs> resourceLogs)
    {
        foreach (var rl in resourceLogs)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rl.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rl.ScopeLogs.Count;
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            application.AddLogs(context, rl.ScopeLogs);
        }

        RaiseSubscriptionChanged(_logSubscriptions);
    }

    public PagedResult<OtlpLogEntry> GetLogs(GetLogsContext context)
    {
        if (!_applications.TryGetValue(context.ApplicationServiceId, out var application))
        {
            return PagedResult<OtlpLogEntry>.Empty;
        }

        return application.GetLogs(context);
    }

    public PagedResult<OtlpTraceScope> GetTraces(GetTracesContext context)
    {
        if (!_applications.TryGetValue(context.ApplicationServiceId, out var application))
        {
            return PagedResult<OtlpTraceScope>.Empty;
        }

        return application.GetTraces(context);
    }

    public List<string>? GetLogPropertyKeys(string applicationServiceId)
    {
        if (!_applications.TryGetValue(applicationServiceId, out var application))
        {
            return null;
        }

        return application.GetLogPropertyKeys();
    }

    internal void AddMetrics(AddContext context, RepeatedField<ResourceMetrics> resourceMetrics)
    {
        foreach (var rm in resourceMetrics)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rm.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rm.ScopeMetrics.Sum(s => s.Metrics.Count);
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            application.AddMetrics(context, rm.ScopeMetrics);
        }

        RaiseSubscriptionChanged(_metricsSubscriptions);
    }

    internal void AddTraces(AddContext context, RepeatedField<ResourceSpans> resourceSpans)
    {
        foreach (var rs in resourceSpans)
        {
            OtlpApplication application;
            try
            {
                application = GetOrAddApplication(rs.Resource);
            }
            catch (Exception ex)
            {
                context.FailureCount += rs.ScopeSpans.Sum(s => s.Spans.Count);
                _logger.LogInformation(ex, "Error adding application.");
                continue;
            }

            application.AddTraces(context, rs.ScopeSpans);
        }

        RaiseSubscriptionChanged(_metricsSubscriptions);
    }
}
