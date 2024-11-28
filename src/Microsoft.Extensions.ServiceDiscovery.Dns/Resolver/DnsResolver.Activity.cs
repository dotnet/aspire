// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.ServiceDiscovery.Dns.Resolver;

internal partial class DnsResolver
{
    internal static class Telemetry
    {
        private static readonly Meter s_meter = new Meter("Microsoft.Extensions.ServiceDiscovery.Dns.Resolver");
        private static readonly Counter<long> s_queryCounter = s_meter.CreateCounter<long>("queries", "Number of DNS queries");
        private static readonly Histogram<double> s_queryDuration = s_meter.CreateHistogram<double>("query.duration", "ms", "DNS query duration");

        public static NameResolutionActivity StartNameResolution(string hostName, QueryType queryType)
        {
            return new NameResolutionActivity(hostName, queryType);
        }

        public static void StopNameResolution(in NameResolutionActivity activity, object? answers, SendQueryError error)
        {
            if (!activity.Stop(answers, error, out TimeSpan duration))
            {
                return;
            }

            s_queryCounter.Add(1);
            s_queryDuration.Record(duration.TotalMilliseconds);
        }
    }

    internal readonly struct NameResolutionActivity
    {
        private const string ActivitySourceName = "Microsoft.Extensions.ServiceDiscovery.Dns.Resolver";
        private const string ActivityName = ActivitySourceName + ".Resolve";
        private static readonly ActivitySource s_activitySource = new ActivitySource(ActivitySourceName);

        private readonly long _startingTimestamp;
        private readonly Activity? _activity;  // null if activity is not started

        public NameResolutionActivity(string hostName, QueryType queryType)
        {
            _startingTimestamp = Stopwatch.GetTimestamp();
            _activity = s_activitySource.StartActivity(ActivityName, ActivityKind.Client);
            if (_activity is not null)
            {
                _activity.DisplayName = $"Resolving {hostName}";
                if (_activity.IsAllDataRequested)
                {
                    _activity.SetTag("dns.question.name", hostName);
                    _activity.SetTag("dns.question.type", queryType.ToString());
                }
            }
        }

        public bool Stop(object? answers, SendQueryError error, out TimeSpan duration)
        {
            if (_activity is null)
            {
                duration = TimeSpan.Zero;
                return false;
            }

            if (_activity.IsAllDataRequested)
            {
                if (answers is not null)
                {
                    static string[] ToStringHelper<T>(T[] array) => array.Select(a => a!.ToString()!).ToArray();

                    string[]? answersArray = answers switch
                    {
                        ServiceResult[] serviceResults => ToStringHelper(serviceResults),
                        AddressResult[] addressResults => ToStringHelper(addressResults),
                        _ => null
                    };

                    Debug.Assert(answersArray is not null);
                    _activity.SetTag("dns.answers", answersArray);
                }
                else
                {
                    _activity.SetTag("error.type", error.ToString());
                }
            }

            if (answers is null)
            {
                _activity.SetStatus(ActivityStatusCode.Error);
            }

            _activity.Stop();
            duration = Stopwatch.GetElapsedTime(_startingTimestamp);
            return true;
        }
    }
}
