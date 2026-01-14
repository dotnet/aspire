// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Ats;
using Aspire.Hosting.RemoteHost.Ats;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class AtsMarshallerTests
{
    private static AtsContext CreateTestContext()
    {
        return new AtsContext
        {
            Capabilities = [],
            HandleTypes = [],
            DtoTypes = [new AtsDtoTypeInfo { TypeId = "test/TestDto", Name = "TestDto", ClrType = typeof(TestDto), Properties = [] }],
            EnumTypes = []
        };
    }

    private static AtsMarshaller CreateTestMarshaller(HandleRegistry? handles = null, CancellationTokenRegistry? ctRegistry = null)
    {
        handles ??= new HandleRegistry();
        ctRegistry ??= new CancellationTokenRegistry();
        var context = CreateTestContext();
        return new AtsMarshaller(handles, context, ctRegistry, new Lazy<AtsCallbackProxyFactory>(() => throw new NotImplementedException()));
    }

    private static AtsMarshaller CreateMarshaller(HandleRegistry? registry = null)
    {
        return CreateTestMarshaller(registry);
    }

    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    public void IsSimpleType_ReturnsTrueForPrimitives(Type type)
    {
        Assert.True(AtsMarshaller.IsSimpleType(type));
    }

    [Fact]
    public void IsSimpleType_ReturnsTrueForNullablePrimitives()
    {
        Assert.True(AtsMarshaller.IsSimpleType(typeof(int?)));
        Assert.True(AtsMarshaller.IsSimpleType(typeof(bool?)));
        Assert.True(AtsMarshaller.IsSimpleType(typeof(DateTime?)));
    }

    [Fact]
    public void IsSimpleType_ReturnsTrueForEnums()
    {
        Assert.True(AtsMarshaller.IsSimpleType(typeof(TestEnum)));
    }

    [Fact]
    public void IsSimpleType_ReturnsFalseForComplexTypes()
    {
        Assert.False(AtsMarshaller.IsSimpleType(typeof(object)));
        Assert.False(AtsMarshaller.IsSimpleType(typeof(List<int>)));
        Assert.False(AtsMarshaller.IsSimpleType(typeof(TestClass)));
    }

    [Fact]
    public void MarshalToJson_ReturnsNullForNull()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(null);

        Assert.Null(result);
    }

    [Fact]
    public void MarshalToJson_MarshalsStringDirectly()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson("hello");

        Assert.NotNull(result);
        Assert.Equal("hello", result.GetValue<string>());
    }

    [Fact]
    public void MarshalToJson_MarshalsIntDirectly()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.GetValue<int>());
    }

    [Fact]
    public void MarshalToJson_MarshalsBoolDirectly()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(true);

        Assert.NotNull(result);
        Assert.True(result.GetValue<bool>());
    }

    [Fact]
    public void MarshalToJson_MarshalsEnumAsString()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(TestEnum.ValueB);

        Assert.NotNull(result);
        Assert.Equal("ValueB", result.GetValue<string>());
    }

    [Fact]
    public void MarshalToJson_MarshalsTimeSpanAsMilliseconds()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(TimeSpan.FromSeconds(1.5));

        Assert.NotNull(result);
        Assert.Equal(1500.0, result.GetValue<double>());
    }

    [Fact]
    public void MarshalToJson_MarshalsArrayRecursively()
    {
        var marshaller = CreateMarshaller();
        var array = new[] { 1, 2, 3 };

        var result = marshaller.MarshalToJson(array);

        Assert.NotNull(result);
        Assert.IsType<JsonArray>(result);
        var jsonArray = (JsonArray)result;
        Assert.Equal(3, jsonArray.Count);
        Assert.Equal(1, jsonArray[0]!.GetValue<int>());
        Assert.Equal(2, jsonArray[1]!.GetValue<int>());
        Assert.Equal(3, jsonArray[2]!.GetValue<int>());
    }

    [Fact]
    public void ConvertPrimitive_ConvertsStringCorrectly()
    {
        var value = JsonValue.Create("test");

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(string));

        Assert.Equal("test", result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsIntCorrectly()
    {
        var value = JsonValue.Create(42);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(int));

        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsBoolCorrectly()
    {
        var value = JsonValue.Create(true);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(bool));

        Assert.Equal(true, result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsTimeSpanFromMilliseconds()
    {
        var value = JsonValue.Create(1500.0);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(TimeSpan));

        Assert.Equal(TimeSpan.FromMilliseconds(1500), result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsNullableInt()
    {
        var value = JsonValue.Create(42);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(int?));

        Assert.Equal(42, result);
    }

    [Fact]
    public void UnmarshalFromJson_ReturnsNullForNullNode()
    {
        var (marshaller, context) = CreateMarshallerWithContext();

        var result = marshaller.UnmarshalFromJson(null, typeof(string), context);

        Assert.Null(result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsHandleReference()
    {
        var registry = new HandleRegistry();
        var obj = new TestClass { Value = 42 };
        var handleId = registry.Register(obj, "aspire/Test");
        var json = new JsonObject { ["$handle"] = handleId };
        var (marshaller, context) = CreateMarshallerWithContext(registry);

        var result = marshaller.UnmarshalFromJson(json, typeof(TestClass), context);

        Assert.Same(obj, result);
    }

    [Fact]
    public void UnmarshalFromJson_ThrowsForUnknownHandle()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonObject { ["$handle"] = "aspire/Unknown:999" };

        Assert.Throws<CapabilityException>(() =>
            marshaller.UnmarshalFromJson(json, typeof(object), context));
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsArray()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonArray { 1, 2, 3 };

        var result = marshaller.UnmarshalFromJson(json, typeof(int[]), context);

        Assert.NotNull(result);
        var array = Assert.IsType<int[]>(result);
        Assert.Equal(new[] { 1, 2, 3 }, array);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsList()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonArray { "a", "b", "c" };

        var result = marshaller.UnmarshalFromJson(json, typeof(List<string>), context);

        Assert.NotNull(result);
        var list = Assert.IsType<List<string>>(result);
        Assert.Equal(["a", "b", "c"], list);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsDictionary()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonObject { ["key1"] = 1, ["key2"] = 2 };

        var result = marshaller.UnmarshalFromJson(json, typeof(Dictionary<string, int>), context);

        Assert.NotNull(result);
        var dict = Assert.IsType<Dictionary<string, int>>(result);
        Assert.Equal(1, dict["key1"]);
        Assert.Equal(2, dict["key2"]);
    }

    // Additional primitive type tests
    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(char))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeSpan))]
    [InlineData(typeof(Uri))]
    public void IsSimpleType_ReturnsTrueForAdditionalPrimitives(Type type)
    {
        Assert.True(AtsMarshaller.IsSimpleType(type));
    }

    // DateOnly marshalling tests
    [Fact]
    public void MarshalToJson_MarshalsDateOnlyAsIsoString()
    {
        var marshaller = CreateMarshaller();
        var dateOnly = new DateOnly(2024, 6, 15);

        var result = marshaller.MarshalToJson(dateOnly);

        Assert.NotNull(result);
        Assert.Equal("2024-06-15", result.GetValue<string>());
    }

    [Fact]
    public void ConvertPrimitive_ConvertsDateOnlyFromString()
    {
        var value = JsonValue.Create("2024-06-15");

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(DateOnly));

        Assert.Equal(new DateOnly(2024, 6, 15), result);
    }

    // TimeOnly marshalling tests
    [Fact]
    public void MarshalToJson_MarshalsTimeOnlyAsIsoString()
    {
        var marshaller = CreateMarshaller();
        var timeOnly = new TimeOnly(14, 30, 45);

        var result = marshaller.MarshalToJson(timeOnly);

        Assert.NotNull(result);
        Assert.Contains("14:30:45", result.GetValue<string>());
    }

    [Fact]
    public void ConvertPrimitive_ConvertsTimeOnlyFromString()
    {
        var value = JsonValue.Create("14:30:45");

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(TimeOnly));

        Assert.Equal(new TimeOnly(14, 30, 45), result);
    }

    [Fact]
    public void MarshalToJson_MarshalsLong()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(9223372036854775807L);

        Assert.NotNull(result);
        Assert.Equal(9223372036854775807L, result.GetValue<long>());
    }

    [Fact]
    public void MarshalToJson_MarshalsDouble()
    {
        var marshaller = CreateMarshaller();

        var result = marshaller.MarshalToJson(3.14159);

        Assert.NotNull(result);
        Assert.Equal(3.14159, result.GetValue<double>());
    }

    [Fact]
    public void MarshalToJson_MarshalsGuid()
    {
        var marshaller = CreateMarshaller();
        var guid = Guid.NewGuid();

        var result = marshaller.MarshalToJson(guid);

        Assert.NotNull(result);
    }

    [Fact]
    public void MarshalToJson_MarshalsListAsHandle()
    {
        var registry = new HandleRegistry();
        var marshaller = CreateMarshaller(registry);
        var list = new List<int> { 1, 2, 3 };

        var result = marshaller.MarshalToJson(list);

        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var jsonObj = (JsonObject)result;
        Assert.NotNull(jsonObj["$handle"]);
        Assert.NotNull(jsonObj["$type"]);
        // Type ID is derived from assembly and type name
        var typeId = jsonObj["$type"]!.GetValue<string>();
        Assert.Contains("List", typeId);
    }

    [Fact]
    public void MarshalToJson_MarshalsDictionaryAsHandle()
    {
        var registry = new HandleRegistry();
        var marshaller = CreateMarshaller(registry);
        var dict = new Dictionary<string, int> { ["a"] = 1 };

        var result = marshaller.MarshalToJson(dict);

        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var jsonObj = (JsonObject)result;
        Assert.NotNull(jsonObj["$handle"]);
        Assert.NotNull(jsonObj["$type"]);
        // Type ID uses special format for dictionary handles: Dict<K,V>
        var typeId = jsonObj["$type"]!.GetValue<string>();
        Assert.Contains("Dict<", typeId);
    }

    [Fact]
    public void MarshalToJson_MarshalsComplexObjectAsHandle()
    {
        var registry = new HandleRegistry();
        var marshaller = CreateMarshaller(registry);
        var obj = new TestClass { Value = 42 };

        var result = marshaller.MarshalToJson(obj);

        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var jsonObj = (JsonObject)result;
        Assert.NotNull(jsonObj["$handle"]);
        Assert.NotNull(jsonObj["$type"]);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsLong()
    {
        var value = JsonValue.Create(9223372036854775807L);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(long));

        Assert.Equal(9223372036854775807L, result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsDouble()
    {
        var value = JsonValue.Create(3.14159);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(double));

        Assert.Equal(3.14159, result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsFloat()
    {
        var value = JsonValue.Create(3.14);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(float));

        Assert.Equal(3.14f, (float)result!, 2);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsDecimal()
    {
        var value = JsonValue.Create(123.456m);

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(decimal));

        Assert.Equal(123.456m, result);
    }

    [Fact]
    public void ConvertPrimitive_ConvertsTimeSpanFromString()
    {
        var value = JsonValue.Create("01:30:00");

        var result = AtsMarshaller.ConvertPrimitive(value!, typeof(TimeSpan));

        Assert.Equal(TimeSpan.FromHours(1.5), result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsIList()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonArray { 1, 2, 3 };

        var result = marshaller.UnmarshalFromJson(json, typeof(IList<int>), context);

        Assert.NotNull(result);
        var list = Assert.IsAssignableFrom<IList<int>>(result);
        Assert.Equal(3, list.Count);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsIEnumerable()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonArray { "x", "y" };

        var result = marshaller.UnmarshalFromJson(json, typeof(IEnumerable<string>), context);

        Assert.NotNull(result);
        var enumerable = Assert.IsAssignableFrom<IEnumerable<string>>(result);
        Assert.Equal(2, enumerable.Count());
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsIDictionary()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonObject { ["a"] = 1, ["b"] = 2 };

        var result = marshaller.UnmarshalFromJson(json, typeof(IDictionary<string, int>), context);

        Assert.NotNull(result);
        var dict = Assert.IsAssignableFrom<IDictionary<string, int>>(result);
        Assert.Equal(2, dict.Count);
    }

    [Fact]
    public void UnmarshalFromJson_ThrowsForNonDtoObject()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonObject { ["value"] = 42 };

        // TestClass doesn't have [AspireDto] attribute
        var ex = Assert.Throws<CapabilityException>(() =>
            marshaller.UnmarshalFromJson(json, typeof(TestClass), context));

        Assert.Contains("AspireDto", ex.Message);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsDto()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = new JsonObject { ["name"] = "test", ["count"] = 5 };

        var result = marshaller.UnmarshalFromJson(json, typeof(TestDto), context);

        Assert.NotNull(result);
        var dto = Assert.IsType<TestDto>(result);
        Assert.Equal("test", dto.Name);
        Assert.Equal(5, dto.Count);
    }

    [Fact]
    public void MarshalToJson_MarshalsDto()
    {
        var marshaller = CreateMarshaller();
        var dto = new TestDto { Name = "test", Count = 10 };

        var result = marshaller.MarshalToJson(dto);

        Assert.NotNull(result);
        Assert.IsType<JsonObject>(result);
        var jsonObj = (JsonObject)result;
        Assert.Equal("test", jsonObj["name"]?.GetValue<string>());
        Assert.Equal(10, jsonObj["count"]?.GetValue<int>());
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsEnumFromString()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = JsonValue.Create("ValueB");

        var result = marshaller.UnmarshalFromJson(json, typeof(TestEnum), context);

        Assert.Equal(TestEnum.ValueB, result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsEnumFromStringCaseInsensitive()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = JsonValue.Create("valueb"); // lowercase

        var result = marshaller.UnmarshalFromJson(json, typeof(TestEnum), context);

        Assert.Equal(TestEnum.ValueB, result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsEnumFromNumericValue()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = JsonValue.Create(1); // ValueB is index 1

        var result = marshaller.UnmarshalFromJson(json, typeof(TestEnum), context);

        Assert.Equal(TestEnum.ValueB, result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsNullableEnumFromString()
    {
        var (marshaller, context) = CreateMarshallerWithContext();
        var json = JsonValue.Create("ValueA");

        var result = marshaller.UnmarshalFromJson(json, typeof(TestEnum?), context);

        Assert.Equal(TestEnum.ValueA, result);
    }

    [Fact]
    public void UnmarshalFromJson_UnmarshalsNullableEnumFromNull()
    {
        var (marshaller, context) = CreateMarshallerWithContext();

        var result = marshaller.UnmarshalFromJson(null, typeof(TestEnum?), context);

        Assert.Null(result);
    }

    private static (AtsMarshaller Marshaller, AtsMarshaller.UnmarshalContext Context) CreateMarshallerWithContext(HandleRegistry? registry = null)
    {
        var handles = registry ?? new HandleRegistry();
        var context = new AtsMarshaller.UnmarshalContext
        {
            CapabilityId = "test/capability",
            ParameterName = "testParam"
        };
        var marshaller = CreateTestMarshaller(handles);
        return (marshaller, context);
    }

    private enum TestEnum
    {
        ValueA,
        ValueB
    }

    private sealed class TestClass
    {
        public int Value { get; set; }
    }

    [AspireDto]
    private sealed class TestDto
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }
}
