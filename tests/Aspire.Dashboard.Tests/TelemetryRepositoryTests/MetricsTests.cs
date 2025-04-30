// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Aspire.Dashboard.Otlp.Storage;
using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using Xunit;
using static Aspire.Tests.Shared.Telemetry.TelemetryTestHelpers;

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
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1)),
                            CreateHistogramMetric(metricName: "test2", startTime: s_testTime.AddMinutes(1))
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

        var instruments = repository.GetInstrumentsSummaries(applications[0].ApplicationKey);
        Assert.Collection(instruments,
            instrument =>
            {
                Assert.Equal("test", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test2", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter2", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test2", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter2", instrument.Parent.Name);
            });
    }

    [Fact]
    public void AddMetrics_MeterAttributeLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16);

        var metricAttributes = new List<KeyValuePair<string, string>>();
        var meterAttributes = new List<KeyValuePair<string, string>>();

        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            metricAttributes.Add(new KeyValuePair<string, string>($"Metric_Key{i}", value));
            meterAttributes.Add(new KeyValuePair<string, string>($"Meter_Key{i}", value));
        }

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
                        Scope = CreateScope(name: "test-meter", attributes: meterAttributes),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: metricAttributes)
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
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.Collection(instrument.Summary.Parent.Attributes,
            p =>
            {
                Assert.Equal("Meter_Key0", p.Key);
                Assert.Equal("01234", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key1", p.Key);
                Assert.Equal("0123456789", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key2", p.Key);
                Assert.Equal("012345678901234", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key3", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key4", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            });

        var dimensionAttributes = instrument.Dimensions.Single().Attributes;

        Assert.Collection(dimensionAttributes,
            p =>
            {
                Assert.Equal("Meter_Key0", p.Key);
                Assert.Equal("01234", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key1", p.Key);
                Assert.Equal("0123456789", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key2", p.Key);
                Assert.Equal("012345678901234", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key3", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            },
            p =>
            {
                Assert.Equal("Meter_Key4", p.Key);
                Assert.Equal("0123456789012345", p.Value);
            });
    }

    [Fact]
    public void AddMetrics_MetricAttributeLimits_LimitsApplied()
    {
        // Arrange
        var repository = CreateRepository(maxAttributeCount: 5, maxAttributeLength: 16);

        var metricAttributes = new List<KeyValuePair<string, string>>();
        var meterAttributes = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("Meter_Key0", GetValue(5))
        };

        for (var i = 0; i < 10; i++)
        {
            var value = GetValue((i + 1) * 5);
            metricAttributes.Add(new KeyValuePair<string, string>($"Metric_Key{i}", value));
        }

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
                        Scope = CreateScope(name: "test-meter", attributes: meterAttributes),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: metricAttributes)
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
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.Collection(instrument.Summary.Parent.Attributes,
            p =>
            {
                Assert.Equal("Meter_Key0", p.Key);
                Assert.Equal("01234", p.Value);
            });

        var dimensionAttributes = instrument.Dimensions.Single().Attributes;

        Assert.Collection(dimensionAttributes,
            p =>
            {
                Assert.Equal("Meter_Key0", p.Key);
                Assert.Equal("01234", p.Value);
            },
            p =>
            {
                Assert.Equal("Metric_Key0", p.Key);
                Assert.Equal("01234", p.Value);
            },
            p =>
            {
                Assert.Equal("Metric_Key1", p.Key);
                Assert.Equal("0123456789", p.Value);
            },
            p =>
            {
                Assert.Equal("Metric_Key2", p.Key);
                Assert.Equal("012345678901234", p.Value);
            },
            p =>
            {
                Assert.Equal("Metric_Key3", p.Key);
                Assert.Equal("0123456789012345", p.Value);
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
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), exemplars: new List<Exemplar> { CreateExemplar(startTime: s_testTime.AddMinutes(1), value: 2, attributes: [KeyValuePair.Create("key1", "value1")]) }),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(2)),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value2")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1")]),
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "")])
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

        var instrumentData = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = s_testTime.AddMinutes(1),
            EndTime = s_testTime.AddMinutes(1.5),
        });

        Assert.NotNull(instrumentData);
        Assert.Equal("test", instrumentData.Summary.Name);
        Assert.Equal("Test metric description", instrumentData.Summary.Description);
        Assert.Equal("widget", instrumentData.Summary.Unit);
        Assert.Equal("test-meter", instrumentData.Summary.Parent.Name);

        Assert.Collection(instrumentData.KnownAttributeValues.OrderBy(kvp => kvp.Key),
            e =>
            {
                Assert.Equal("key1", e.Key);
                Assert.Equal(new[] { null, "value1", "value2" }, e.Value);
            },
            e =>
            {
                Assert.Equal("key2", e.Key);
                Assert.Equal(new[] { null, "value1", "" }, e.Value);
            });

        Assert.Equal(5, instrumentData.Dimensions.Count);

        var dimension = instrumentData.Dimensions.Single(d => d.Attributes.Length == 0);
        var exemplar = Assert.Single(dimension.Values[0].Exemplars);

        Assert.Equal("key1", exemplar.Attributes[0].Key);
        Assert.Equal("value1", exemplar.Attributes[0].Value);

        var instrument = applications.Single().GetInstrument("test-meter", "test", s_testTime.AddMinutes(1), s_testTime.AddMinutes(1.5));
        Assert.NotNull(instrument);

        AssertDimensionValues(instrument.Dimensions, Array.Empty<KeyValuePair<string, string>>(), valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value2") }, valueCount: 1);
        AssertDimensionValues(instrument.Dimensions, new KeyValuePair<string, string>[] { KeyValuePair.Create("key1", "value1"), KeyValuePair.Create("key2", "value1") }, valueCount: 1);
    }

    private static Exemplar CreateExemplar(DateTime startTime, double value, IEnumerable<KeyValuePair<string, string>>? attributes = null)
    {
        var exemplar = new Exemplar
        {
            TimeUnixNano = DateTimeToUnixNanoseconds(startTime),
            AsDouble = value,
            SpanId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("span-id")),
            TraceId = ByteString.CopyFrom(Encoding.UTF8.GetBytes("trace-id"))
        };

        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                exemplar.FilteredAttributes.Add(new KeyValue { Key = attribute.Key, Value = new AnyValue { StringValue = attribute.Value } });
            }
        }

        return exemplar;
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
            ApplicationKey = applications[0].ApplicationKey,
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        })!;

        Assert.Equal("test", instrument.Summary.Name);
        Assert.Equal("Test metric description", instrument.Summary.Description);
        Assert.Equal("widget", instrument.Summary.Unit);
        Assert.Equal("test-meter", instrument.Summary.Parent.Name);

        // Only the last 3 values should be kept.
        var dimension = Assert.Single(instrument.Dimensions);
        Assert.Collection(dimension.Values,
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

    [Fact]
    public void GetMetrics_MultipleInstances()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")])
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-3")]),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-4")])
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-5")]),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-6")])
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var appKey = new ApplicationKey("app1", InstanceId: null);
        var instruments = repository.GetInstrumentsSummaries(appKey);
        Assert.Collection(instruments,
            instrument =>
            {
                Assert.Equal("test1", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test2", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            });

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = appKey,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(instrument);
        Assert.Equal("test1", instrument.Summary.Name);

        Assert.Collection(instrument.Dimensions.OrderBy(d => d.Name),
            d =>
            {
                Assert.Equal(KeyValuePair.Create("key-1", "value-1"), d.Attributes.Single());
                Assert.Equal(1, ((MetricValue<long>)d.Values.Single()).Value);
            },
            d =>
            {
                Assert.Equal(KeyValuePair.Create("key-1", "value-2"), d.Attributes.Single());
                Assert.Equal(2, ((MetricValue<long>)d.Values.Single()).Value);
            },
            d =>
            {
                Assert.Equal(KeyValuePair.Create("key-1", "value-3"), d.Attributes.Single());
                Assert.Equal(3, ((MetricValue<long>)d.Values.Single()).Value);
            });

        var knownValues = Assert.Single(instrument.KnownAttributeValues);
        Assert.Equal("key-1", knownValues.Key);

        Assert.Collection(knownValues.Value.Order(),
            v => Assert.Equal("value-1", v),
            v => Assert.Equal("value-2", v),
            v => Assert.Equal("value-3", v));
    }

    [Fact]
    public void RemoveMetrics_All()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics();

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.Empty(app1Instruments);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);

        Assert.Empty(app2Instruments);
    }

    [Fact]
    public void RemoveMetrics_SelectedResource()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics(new ApplicationKey("app1", "456"));

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);

        var app1Instrument = Assert.Single(app1Instruments);
        Assert.Equal("test1", app1Instrument.Name);
        Assert.Equal("Test metric description", app1Instrument.Description);
        Assert.Equal("widget", app1Instrument.Unit);
        Assert.Equal("test-meter", app1Instrument.Parent.Name);

        var app1Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(app1Test1Instrument);
        Assert.Equal("test1", app1Test1Instrument.Summary.Name);

        var app1Test1Dimensions = Assert.Single(app1Test1Instrument.Dimensions);
        Assert.Collection(app1Test1Dimensions.Values,
            v =>
            {
                Assert.Equal(1, ((MetricValue<long>)v).Value);
            },
            v =>
            {
                Assert.Equal(2, ((MetricValue<long>)v).Value);
            });

        var app1Test2Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test2",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.Null(app1Test2Instrument);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);

        Assert.Collection(app2Instruments,
            instrument =>
            {
                Assert.Equal("test1", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test3", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            });

        var app2Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(app2Test1Instrument);
        Assert.Equal("test1", app2Test1Instrument.Summary.Name);

        var app2Test1Dimensions = Assert.Single(app2Test1Instrument.Dimensions);
        Assert.Equal(5, ((MetricValue<long>)app2Test1Dimensions.Values.Single()).Value);

        var app2Test3Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test3",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(app2Test3Instrument);
        Assert.Equal("test3", app2Test3Instrument.Summary.Name);

        var app2Test3Dimensions = Assert.Single(app2Test3Instrument.Dimensions);
        Assert.Equal(6, ((MetricValue<long>)app2Test3Dimensions.Values.Single()).Value);
    }

    [Fact]
    public void RemoveMetrics_MultipleSelectedResources()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")]),
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "456"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 3, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test2", value: 4, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            },
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app2"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "test1", value: 5, startTime: s_testTime.AddMinutes(1)),
                            CreateSumMetric(metricName: "test3", value: 6, startTime: s_testTime.AddMinutes(1))
                        }
                    }
                }
            }
        });

        // Act
        repository.ClearMetrics(new ApplicationKey("app1", null));

        // Assert
        Assert.Equal(0, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.Empty(app1Instruments);

        var app1Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.Null(app1Test1Instrument);

        var app1Test2Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app1Key,
            InstrumentName = "test2",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.Null(app1Test2Instrument);

        var app2Key = new ApplicationKey("app2", InstanceId: null);
        var app2Instruments = repository.GetInstrumentsSummaries(app2Key);
        Assert.Collection(app2Instruments,
            instrument =>
            {
                Assert.Equal("test1", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            },
            instrument =>
            {
                Assert.Equal("test3", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            });

        var app2Test1Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test1",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(app2Test1Instrument);
        Assert.Equal("test1", app2Test1Instrument.Summary.Name);

        var app2Test1Dimensions = Assert.Single(app2Test1Instrument.Dimensions);
        Assert.Equal(5, ((MetricValue<long>)app2Test1Dimensions.Values.Single()).Value);

        var app2Test3Instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = app2Key,
            InstrumentName = "test3",
            MeterName = "test-meter",
            StartTime = s_testTime,
            EndTime = s_testTime.AddMinutes(20)
        });

        Assert.NotNull(app2Test3Instrument);
        Assert.Equal("test3", app2Test3Instrument.Summary.Name);

        var app2Test3Dimensions = Assert.Single(app2Test3Instrument.Dimensions);
        Assert.Equal(6, ((MetricValue<long>)app2Test3Dimensions.Values.Single()).Value);
    }

    [Fact]
    public void AddMetrics_InvalidInstrument()
    {
        // Arrange
        var repository = CreateRepository();

        var addContext = new AddContext();

        // Act
        repository.AddMetrics(addContext, new RepeatedField<ResourceMetrics>()
        {
            new ResourceMetrics
            {
                Resource = CreateResource(name: "app1", instanceId: "123"),
                ScopeMetrics =
                {
                    new ScopeMetrics
                    {
                        Scope = CreateScope(name: "test-meter"),
                        Metrics =
                        {
                            CreateSumMetric(metricName: "", value: 1, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-1")]),
                            CreateSumMetric(metricName: "test1", value: 2, startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("key-1", "value-2")]),
                        }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(1, addContext.FailureCount);

        var app1Key = new ApplicationKey("app1", InstanceId: null);
        var app1Instruments = repository.GetInstrumentsSummaries(app1Key);
        Assert.Collection(app1Instruments,
            instrument =>
            {
                Assert.Equal("test1", instrument.Name);
                Assert.Equal("Test metric description", instrument.Description);
                Assert.Equal("widget", instrument.Unit);
                Assert.Equal("test-meter", instrument.Parent.Name);
            });
    }

    [Fact]
    public void AddMetrics_InvalidHistogramDataPoints()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var addContext = new AddContext();

        var histogramMetric = new Metric
        {
            Name = "test",
            Description = "Test metric description",
            Unit = "widget",
            Histogram = new Histogram
            {
                AggregationTemporality = AggregationTemporality.Cumulative,
                DataPoints =
                {
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { },
                        BucketCounts = { 1 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(1))
                    },
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { },
                        BucketCounts = { 1 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(2))
                    },
                    new HistogramDataPoint
                    {
                        Count = 6,
                        Sum = 1,
                        ExplicitBounds = { 1, 2, 3 },
                        BucketCounts = { 1, 2, 3 },
                        TimeUnixNano = DateTimeToUnixNanoseconds(s_testTime.AddMinutes(3))
                    }
                }
            }
        };

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
                        Metrics = { histogramMetric }
                    }
                }
            }
        });

        // Assert
        Assert.Equal(2, addContext.FailureCount);

        var applications = Assert.Single(repository.GetApplications());

        var instrument = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = applications.ApplicationKey,
            MeterName = "test-meter",
            InstrumentName = "test",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.NotNull(instrument);
        Assert.Equal("test", instrument.Summary.Name);
        Assert.Equal("Test metric description", instrument.Summary.Description);
        Assert.Equal("widget", instrument.Summary.Unit);
        Assert.Equal("test-meter", instrument.Summary.Parent.Name);

        var dimension = Assert.Single(instrument.Dimensions);
        Assert.Single(dimension.Values);
    }

    [Fact]
    public void AddMetrics_OverflowDimension()
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
                            CreateSumMetric(metricName: "test", startTime: s_testTime.AddMinutes(1), attributes: [KeyValuePair.Create("otel.metric.overflow", "true")])
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

        var instrument1 = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            InstrumentName = "test",
            MeterName = "test-meter",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.NotNull(instrument1);
        Assert.True(instrument1.HasOverflow);

        var instrument2 = repository.GetInstrument(new GetInstrumentRequest
        {
            ApplicationKey = new ApplicationKey("TestService", "TestId"),
            InstrumentName = "test",
            MeterName = "test-meter2",
            StartTime = DateTime.MinValue,
            EndTime = DateTime.MaxValue
        });

        Assert.NotNull(instrument2);
        Assert.False(instrument2.HasOverflow);
    }

    private static void AssertDimensionValues(Dictionary<ReadOnlyMemory<KeyValuePair<string, string>>, DimensionScope> dimensions, ReadOnlyMemory<KeyValuePair<string, string>> key, int valueCount)
    {
        var scope = dimensions[key];
        Assert.True(Enumerable.SequenceEqual(MemoryMarshal.ToEnumerable(key), scope.Attributes), "Key and attributes don't match.");

        Assert.Equal(valueCount, scope.Values.Count);
    }
}
