// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using static Aspire.Dashboard.Tests.TelemetryRepositoryTests.TestHelpers;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class MetricsTests
{
    private static readonly DateTime s_testTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AddMetrics()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2)),
                            CreateSumMetric(metricName: "test2", startTime: s_testTime.AddMinutes(1)),
                        }
                    },
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter2"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var instruments = repository.GetInstrumentsSummary(applications[0].InstanceId);
        Assert.Collection(instruments,
            instrument =>
            {
                Assert.Equal("test", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.MeterName);
                Assert.Empty(instrument.Dimensions);
                Assert.Empty(instrument.KnownAttributeValues);
            },
            instrument =>
            {
                Assert.Equal("test2", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.MeterName);
                Assert.Empty(instrument.Dimensions);
                Assert.Empty(instrument.KnownAttributeValues);
            },
            instrument =>
            {
                Assert.Equal("test", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter2", instrument.Parent.MeterName);
                Assert.Empty(instrument.Dimensions);
                Assert.Empty(instrument.KnownAttributeValues);
            });
    }

    [Fact]
    public void RoundtripSeconds()
    {
        var start = s_testTime.AddMinutes(1);
        var nanoSeconds = DateTimeToUnixNanoseconds(start);
        var end = OtlpHelpers.UnixNanoSecondsToDateTime(nanoSeconds);
        Assert.Equal(start, end);
    }

    [Fact]
    public void GetInstrument()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value2")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1")])
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationServiceId = applications[0].InstanceId,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = s_testTime.AddMinutes(1),
            EndTime = s_testTime.AddMinutes(1.5),
        });

        Assert.NotNull(instrument);
        Assert.Equal("test", instrument.Name);
        Assert.Equal("Test metric description", instrument.Description);
        Assert.Equal("widget", instrument.Unit);
        Assert.Equal("test-meter", instrument.Parent.MeterName);

        Assert.Collection(instrument.KnownAttributeValues.OrderBy(kvp => kvp.Key),
            e =>
            {
                Assert.Equal("key1", e.Key);
                Assert.Equal(new [] { "", "value1", "value2" }, e.Value);
            },
            e =>
            {
                Assert.Equal("key2", e.Key);
                Assert.Equal(new[] { "", "value1" }, e.Value);
            });

        Assert.Equal(4, instrument.Dimensions.Count);

        AssertDimensionValues(instrument.Dimensions, Array.Empty<KeyValuePair<string, string>>(), valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value2") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1") }, valueCount: 1);
    }

    [Fact]
    public void AddMetrics_Capacity_ValuesRemoved()
    {
        // Arrange
        var repository = CreateRepository(maxMetricsCount: 3);

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), value: 1),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2), value: 2),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(3), value: 3),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(4), value: 4),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(5), value: 5),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var applications = repository.GetApplications();
        Assert.Collection(applications,
            app =>
            {
                Assert.Equal("TestService", app.ApplicationName);
                Assert.Equal("TestId", app.InstanceId);
            });

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationServiceId = applications[0].InstanceId,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.Equal("test", instrument.Name);
        Assert.Equal("Test metric description", instrument.Description);
        Assert.Equal("widget", instrument.Unit);
        Assert.Equal("test-meter", instrument.Parent.MeterName);

        // Only the last 3 values should be kept.
        var dimension = Assert.Single(instrument.Dimensions);
        Assert.Collection(dimension.Value.Values,
            m =>
            {
                Assert.Equal(s_testTime.AddMinutes(2), m.Start);
                Assert.Equal(s_testTime.AddMinutes(3), m.End);
                Assert.Equal(3, ((MetricValue<long>)m).Value);
            },
            m =>
            {
                Assert.Equal(s_testTime.AddMinutes(3), m.Start);
                Assert.Equal(s_testTime.AddMinutes(4), m.End);
                Assert.Equal(4, ((MetricValue<long>)m).Value);
            },
            m =>
            {
                Assert.Equal(s_testTime.AddMinutes(4), m.Start);
                Assert.Equal(s_testTime.AddMinutes(5), m.End);
                Assert.Equal(5, ((MetricValue<long>)m).Value);
            });
    }

    private static void AssertDimensionValues(Dictionary<ReadOnlyMemory<KeyValuePair<string, string>>, DimensionScope> dimensions, ReadOnlyMemory<KeyValuePair<string, string>> key, int valueCount)
    {
        var scope = dimensions[key];
        Assert.True(Enumerable.SequenceEqual(key.ToArray(), scope.Attributes), "Key and attributes don't match.");

        Assert.Equal(valueCount, scope.Values.Count);
    }
}
