// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Templating.Git;

namespace Aspire.Cli.Tests.Templating.Git;

public class JsonObjectConverterTests
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonObjectConverter() }
    };

    #region Read

    [Fact]
    public void Read_StringValue_ReturnsString()
    {
        var result = JsonSerializer.Deserialize<object>("\"hello\"", s_options);
        Assert.IsType<string>(result);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Read_TrueValue_ReturnsBool()
    {
        var result = JsonSerializer.Deserialize<object>("true", s_options);
        Assert.IsType<bool>(result);
        Assert.Equal(true, result);
    }

    [Fact]
    public void Read_FalseValue_ReturnsBool()
    {
        var result = JsonSerializer.Deserialize<object>("false", s_options);
        Assert.IsType<bool>(result);
        Assert.Equal(false, result);
    }

    [Fact]
    public void Read_IntegerValue_ReturnsInt()
    {
        var result = JsonSerializer.Deserialize<object>("42", s_options);
        Assert.IsType<int>(result);
        Assert.Equal(42, result);
    }

    [Fact]
    public void Read_NegativeInteger_ReturnsInt()
    {
        var result = JsonSerializer.Deserialize<object>("-1", s_options);
        Assert.IsType<int>(result);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Read_DoubleValue_ReturnsDouble()
    {
        var result = JsonSerializer.Deserialize<object>("3.14", s_options);
        Assert.IsType<double>(result);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void Read_NullValue_ReturnsNull()
    {
        var result = JsonSerializer.Deserialize<object>("null", s_options);
        Assert.Null(result);
    }

    [Fact]
    public void Read_EmptyString_ReturnsEmptyString()
    {
        var result = JsonSerializer.Deserialize<object>("\"\"", s_options);
        Assert.Equal("", result);
    }

    [Fact]
    public void Read_ZeroValue_ReturnsInt()
    {
        var result = JsonSerializer.Deserialize<object>("0", s_options);
        Assert.IsType<int>(result);
        Assert.Equal(0, result);
    }

    #endregion

    #region Write

    [Fact]
    public void Write_String_WritesStringJson()
    {
        var json = JsonSerializer.Serialize<object>("hello", s_options);
        Assert.Equal("\"hello\"", json);
    }

    [Fact]
    public void Write_Bool_WritesBoolJson()
    {
        var json = JsonSerializer.Serialize<object>(true, s_options);
        Assert.Equal("true", json);
    }

    [Fact]
    public void Write_Int_WritesNumberJson()
    {
        var json = JsonSerializer.Serialize<object>(42, s_options);
        Assert.Equal("42", json);
    }

    [Fact]
    public void Write_Null_WritesNull()
    {
        var json = JsonSerializer.Serialize<object?>(null, s_options);
        Assert.Equal("null", json);
    }

    #endregion
}

public class JsonObjectListConverterTests
{
    private sealed class TestHolder
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonObjectListConverter))]
        public List<object>? Values { get; set; }
    }

    private static readonly JsonSerializerOptions s_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region Read

    [Fact]
    public void Read_MixedArray_ParsesAllTypes()
    {
        var json = """{"values": ["hello", true, false, 42]}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.NotNull(result?.Values);
        Assert.Equal(4, result.Values.Count);
        Assert.Equal("hello", result.Values[0]);
        Assert.Equal(true, result.Values[1]);
        Assert.Equal(false, result.Values[2]);
        Assert.Equal(42, result.Values[3]);
    }

    [Fact]
    public void Read_EmptyArray_ReturnsEmptyList()
    {
        var json = """{"values": []}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);
        Assert.NotNull(result?.Values);
        Assert.Empty(result.Values);
    }

    [Fact]
    public void Read_NullArray_ReturnsNull()
    {
        var json = """{"values": null}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);
        Assert.Null(result?.Values);
    }

    [Fact]
    public void Read_StringOnlyArray_Works()
    {
        var json = """{"values": ["a", "b", "c"]}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.Equal(3, result!.Values!.Count);
        Assert.All(result.Values, v => Assert.IsType<string>(v));
    }

    [Fact]
    public void Read_BoolOnlyArray_Works()
    {
        var json = """{"values": [true, false]}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.Equal(2, result!.Values!.Count);
        Assert.Equal(true, result.Values[0]);
        Assert.Equal(false, result.Values[1]);
    }

    [Fact]
    public void Read_IntOnlyArray_Works()
    {
        var json = """{"values": [1, 2, 3]}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.Equal(3, result!.Values!.Count);
        Assert.All(result.Values, v => Assert.IsType<int>(v));
    }

    [Fact]
    public void Read_NullElementsInArray_SkipsNulls()
    {
        var json = """{"values": ["a", null, "b"]}""";
        var result = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.Equal(2, result!.Values!.Count);
        Assert.Equal("a", result.Values[0]);
        Assert.Equal("b", result.Values[1]);
    }

    #endregion

    #region Write

    [Fact]
    public void Write_MixedList_ProducesValidJson()
    {
        var holder = new TestHolder { Values = ["hello", true, 42] };
        var json = JsonSerializer.Serialize(holder, s_options);

        Assert.Contains("\"hello\"", json);
        Assert.Contains("true", json);
        Assert.Contains("42", json);
    }

    [Fact]
    public void Write_NullList_ProducesNull()
    {
        var holder = new TestHolder { Values = null };
        var json = JsonSerializer.Serialize(holder, s_options);
        Assert.Contains("null", json);
    }

    [Fact]
    public void Write_EmptyList_ProducesEmptyArray()
    {
        var holder = new TestHolder { Values = [] };
        var json = JsonSerializer.Serialize(holder, s_options);
        Assert.Contains("[]", json);
    }

    #endregion

    #region Roundtrip

    [Fact]
    public void Roundtrip_MixedValues_PreservesTypes()
    {
        var original = new TestHolder { Values = ["str", true, false, 100] };
        var json = JsonSerializer.Serialize(original, s_options);
        var deserialized = JsonSerializer.Deserialize<TestHolder>(json, s_options);

        Assert.NotNull(deserialized?.Values);
        Assert.Equal(4, deserialized.Values.Count);
        Assert.IsType<string>(deserialized.Values[0]);
        Assert.IsType<bool>(deserialized.Values[1]);
        Assert.IsType<bool>(deserialized.Values[2]);
        Assert.IsType<int>(deserialized.Values[3]);
    }

    #endregion
}
