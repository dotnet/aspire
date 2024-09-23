// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Google.Protobuf;
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
}
