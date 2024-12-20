// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Internal;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Instrumentation.ConfluentKafka;

internal static class ConfluentKafkaCommon
{
    internal const string ReceiveOperationName = "receive";
    internal const string ProcessOperationName = "process";
    internal const string KafkaMessagingSystem = "kafka";
    internal const string PublishOperationName = "publish";

    internal const string InstrumentationName = "OpenTelemetry.Instrumentation.ConfluentKafka";
    internal static readonly string InstrumentationVersion = new Version(0, 1, 0, 0).ToString();
    internal static readonly ActivitySource ActivitySource = new(InstrumentationName, InstrumentationVersion);
    internal static readonly Meter Meter = new(InstrumentationName, InstrumentationVersion);
    internal static readonly Counter<long> ReceiveMessagesCounter = Meter.CreateCounter<long>(SemanticConventions.MetricMessagingReceiveMessages);
    internal static readonly Histogram<double> ReceiveDurationHistogram = Meter.CreateHistogram<double>(SemanticConventions.MetricMessagingReceiveDuration);
    internal static readonly Counter<long> PublishMessagesCounter = Meter.CreateCounter<long>(SemanticConventions.MetricMessagingPublishMessages);
    internal static readonly Histogram<double> PublishDurationHistogram = Meter.CreateHistogram<double>(SemanticConventions.MetricMessagingPublishDuration);
}
