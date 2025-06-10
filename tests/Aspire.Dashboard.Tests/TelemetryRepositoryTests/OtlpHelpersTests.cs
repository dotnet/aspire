// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Otlp.Model;
using Aspire.Tests.Shared.Telemetry;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using OpenTelemetry.Proto.Common.V1;
using Xunit;

namespace Aspire.Dashboard.Tests.TelemetryRepositoryTests;

public class OtlpHelpersTests
{
    [Fact]
    public void GetString_StringValue()
    {
        // Arrange
        var anyValue = new AnyValue { StringValue = "string!" };

        // Act
        var s = OtlpHelpers.GetString(anyValue);

        // Assert
        Assert.Equal("string!", s);
    }

    [Fact]
    public void GetString_BytesValue()
    {
        // Arrange
        var anyValue = new AnyValue
        {
            BytesValue = ByteString.CopyFromUtf8("Hello world")
        };

        // Act
        var s = OtlpHelpers.GetString(anyValue);

        // Assert
        Assert.Equal("48656c6c6f20776f726c64", s);
    }

    [Fact]
    public void GetString_NoneValue()
    {
        // Arrange
        var anyValue = new AnyValue();

        // Act
        var s = OtlpHelpers.GetString(anyValue);

        // Assert
        Assert.Equal("", s);
    }

    [Fact]
    public void GetString_NullValue()
    {
        // Arrange
        AnyValue? anyValue = null;

        // Act
        var s = anyValue.GetString();

        // Assert
        Assert.Equal("", s);
    }

    [Fact]
    public void GetString_ArrayValue()
    {
        // Arrange
        var anyValue = new AnyValue
        {
            ArrayValue = new ArrayValue
            {
                Values =
                {
                    new AnyValue { BoolValue = true },
                    new AnyValue()
                }
            }
        };

        // Act
        var s = OtlpHelpers.GetString(anyValue);

        // Assert
        Assert.Equal("[true,null]", s);
    }

    [Fact]
    public void GetString_KeyValues()
    {
        // Arrange
        var anyValue = new AnyValue
        {
            KvlistValue = new KeyValueList
            {
                Values =
                {
                    new KeyValue
                    {
                        Key = "prop1",
                        Value = new AnyValue
                        {
                            ArrayValue = new ArrayValue
                            {
                                Values =
                                {
                                    new AnyValue { StringValue = "string!" },
                                    new AnyValue { DoubleValue = 1.1d },
                                    new AnyValue { IntValue = 1 }
                                }
                            }
                        }
                    },
                    new KeyValue
                    {
                        Key = "prop2",
                        Value = new AnyValue
                        {
                            BytesValue = ByteString.CopyFromUtf8("Hello world")
                        }
                    },
                    new KeyValue
                    {
                        Key = "prop3",
                        Value = new AnyValue
                        {
                            KvlistValue = new KeyValueList
                            {
                                Values =
                                {
                                    new KeyValue
                                    {
                                        Key = "nestedProp1",
                                        Value = new AnyValue { StringValue = "nested!" }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var s = OtlpHelpers.GetString(anyValue);

        // Assert
        Assert.Equal(@"{""prop1"":[""string!"",1.1,1],""prop2"":""48656c6c6f20776f726c64"",""prop3"":{""nestedProp1"":""nested!""}}", s);
    }

    [Fact]
    public void CopyKeyValuePairs_UnderLimit_AllCopied()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } }
            },
            [],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(1, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1", a.Value);
            });
    }

    [Fact]
    public void CopyKeyValuePairs_OverLimit_LimitCopied()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } },
                new KeyValue { Key = "key4", Value = new AnyValue { StringValue = "value4" } }
            },
            [],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(3, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2", a.Value);
            },
            a =>
            {
                Assert.Equal("key3", a.Key);
                Assert.Equal("value3", a.Value);
            });
    }

    [Fact]
    public void CopyKeyValuePairs_OverLimitWithDuplicates_LimitCopied()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1-2" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2-2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3-2" } },
                new KeyValue { Key = "key4", Value = new AnyValue { StringValue = "value4" } },
                new KeyValue { Key = "key4", Value = new AnyValue { StringValue = "value4-2" } }
            },
            [],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(3, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1-2", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2-2", a.Value);
            },
            a =>
            {
                Assert.Equal("key3", a.Key);
                Assert.Equal("value3-2", a.Value);
            });
    }

    [Fact]
    public void CopyKeyValuePairs_HasParent_UnderLimit_LimitCopied()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } }
            },
            [
                new KeyValuePair<string, string>("parentkey1", "parentvalue1")
            ],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(2, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("parentkey1", a.Key);
                Assert.Equal("parentvalue1", a.Value);
            },
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1", a.Value);
            });
    }

    [Fact]
    public void CopyKeyValuePairs_HasParent_OverLimit_LimitCopied()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } }
            },
            [
                new KeyValuePair<string, string>("parentkey1", "parentvalue1")
            ],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(3, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("parentkey1", a.Key);
                Assert.Equal("parentvalue1", a.Value);
            },
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2", a.Value);
            });
    }

    [Fact]
    public void CopyKeyValuePairs_HasParent_ParentLimit_ParentValues()
    {
        // Arrange
        KeyValuePair<string, string>[]? copiedAttributes = null;

        // Act
        OtlpHelpers.CopyKeyValuePairs(
            new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } }
            },
            [
                new KeyValuePair<string, string>("parentkey1", "parentvalue1"),
                new KeyValuePair<string, string>("parentkey2", "parentvalue2"),
                new KeyValuePair<string, string>("parentkey3", "parentvalue3")
            ],
            TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }),
            out var copyCount,
            ref copiedAttributes);

        // Assert
        Assert.Equal(3, copyCount);
        Assert.Collection(copiedAttributes,
            a =>
            {
                Assert.Equal("parentkey1", a.Key);
                Assert.Equal("parentvalue1", a.Value);
            },
            a =>
            {
                Assert.Equal("parentkey2", a.Key);
                Assert.Equal("parentvalue2", a.Value);
            },
            a =>
            {
                Assert.Equal("parentkey3", a.Key);
                Assert.Equal("parentvalue3", a.Value);
            });
    }

    [Fact]
    public void ToKeyValuePairs_OverLimit_LimitReturned()
    {
        // Arrange
        var attributes = new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } }
            };

        // Act
        var results = attributes.ToKeyValuePairs(TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 2 }));

        // Act
        Assert.Collection(results,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2", a.Value);
            });
    }

    [Fact]
    public void ToKeyValuePairs_OverLimitWithDuplicates_LimitReturned()
    {
        // Arrange
        var attributes = new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1-2" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } }
            };

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddProvider(new TestLoggerProvider(testSink));
        });

        // Act
        var context = TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 2 }, logger: factory.CreateLogger<OtlpHelpersTests>());
        var results = attributes.ToKeyValuePairs(context);

        // Assert
        Assert.Collection(results,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1-2", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2", a.Value);
            });

        var w = Assert.Single(testSink.Writes);
        Assert.Equal("Duplicate attribute key1 with different value. Last value wins.", w.Message);
    }

    [Fact]
    public void ToKeyValuePairs_OverLimitWithDuplicates_Filter_LimitReturned()
    {
        // Arrange
        var attributes = new RepeatedField<KeyValue>
            {
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1" } },
                new KeyValue { Key = "key1", Value = new AnyValue { StringValue = "value1-2" } },
                new KeyValue { Key = "key1-2", Value = new AnyValue { StringValue = "value1-2" } },
                new KeyValue { Key = "key1-3", Value = new AnyValue { StringValue = "value1-3" } },
                new KeyValue { Key = "key1-4", Value = new AnyValue { StringValue = "value1-4" } },
                new KeyValue { Key = "key1-5", Value = new AnyValue { StringValue = "value1-5" } },
                new KeyValue { Key = "key2", Value = new AnyValue { StringValue = "value2" } },
                new KeyValue { Key = "key3", Value = new AnyValue { StringValue = "value3" } },
                new KeyValue { Key = "key4", Value = new AnyValue { StringValue = "value4" } }
            };

        var testSink = new TestSink();
        var factory = LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Debug);
            b.AddProvider(new TestLoggerProvider(testSink));
        });

        // Act
        var context = TelemetryTestHelpers.CreateContext(options: new TelemetryLimitOptions { MaxAttributeCount = 3 }, logger: factory.CreateLogger<OtlpHelpersTests>());
        var results = attributes.ToKeyValuePairs(context, kv =>
        {
            return !kv.Key.Contains('-');
        });

        // Assert
        Assert.Collection(results,
            a =>
            {
                Assert.Equal("key1", a.Key);
                Assert.Equal("value1-2", a.Value);
            },
            a =>
            {
                Assert.Equal("key2", a.Key);
                Assert.Equal("value2", a.Value);
            },
            a =>
            {
                Assert.Equal("key3", a.Key);
                Assert.Equal("value3", a.Value);
            });

        var w = Assert.Single(testSink.Writes);
        Assert.Equal("Duplicate attribute key1 with different value. Last value wins.", w.Message);
    }
}
