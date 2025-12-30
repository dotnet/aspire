// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class ObjectRegistryTests
{
    [Fact]
    public void Register_ReturnsUniqueIds()
    {
        var registry = new ObjectRegistry();
        var obj1 = new object();
        var obj2 = new object();

        var id1 = registry.Register(obj1);
        var id2 = registry.Register(obj2);

        Assert.NotEqual(id1, id2);
        Assert.StartsWith("obj_", id1);
        Assert.StartsWith("obj_", id2);
    }

    [Fact]
    public void TryGet_ReturnsRegisteredObject()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test" };
        var id = registry.Register(obj);

        var found = registry.TryGet(id, out var result);

        Assert.True(found);
        Assert.Same(obj, result);
    }

    [Fact]
    public void TryGet_ReturnsFalseForUnknownId()
    {
        var registry = new ObjectRegistry();

        var found = registry.TryGet("unknown_id", out var result);

        Assert.False(found);
        Assert.Null(result);
    }

    [Fact]
    public void Get_ReturnsRegisteredObject()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test" };
        var id = registry.Register(obj);

        var result = registry.Get(id);

        Assert.Same(obj, result);
    }

    [Fact]
    public void Get_ThrowsForUnknownId()
    {
        var registry = new ObjectRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Get("unknown_id"));
        Assert.Contains("unknown_id", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Unregister_RemovesObject()
    {
        var registry = new ObjectRegistry();
        var obj = new object();
        var id = registry.Register(obj);

        var removed = registry.Unregister(id);

        Assert.True(removed);
        Assert.False(registry.TryGet(id, out _));
    }

    [Fact]
    public void Unregister_ReturnsFalseForUnknownId()
    {
        var registry = new ObjectRegistry();

        var removed = registry.Unregister("unknown_id");

        Assert.False(removed);
    }

    [Fact]
    public void Clear_RemovesAllObjects()
    {
        var registry = new ObjectRegistry();
        var id1 = registry.Register(new object());
        var id2 = registry.Register(new object());

        registry.Clear();

        Assert.Equal(0, registry.Count);
        Assert.False(registry.TryGet(id1, out _));
        Assert.False(registry.TryGet(id2, out _));
    }

    [Fact]
    public void Count_ReturnsNumberOfRegisteredObjects()
    {
        var registry = new ObjectRegistry();

        Assert.Equal(0, registry.Count);

        registry.Register(new object());
        Assert.Equal(1, registry.Count);

        registry.Register(new object());
        Assert.Equal(2, registry.Count);
    }

    [Theory]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(DateTimeOffset), true)]
    [InlineData(typeof(TimeSpan), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(TestEnum), true)]
    [InlineData(typeof(TestClass), false)]
    [InlineData(typeof(List<string>), false)]
    [InlineData(typeof(Dictionary<string, int>), false)]
    public void IsSimpleType_ClassifiesTypesCorrectly(Type type, bool expected)
    {
        var result = ObjectRegistry.IsSimpleType(type);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Marshal_IncludesIdAndType()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test", Value = 42 };

        var marshalled = registry.Marshal(obj);

        Assert.True(marshalled.ContainsKey("$id"));
        Assert.Equal(typeof(TestClass).FullName, marshalled["$type"]);
        Assert.Equal(2, marshalled.Count); // Only $id and $type
    }

    [Fact]
    public void Marshal_RegistersObjectInRegistry()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test" };

        var marshalled = registry.Marshal(obj);

        var id = marshalled["$id"] as string;
        Assert.NotNull(id);
        Assert.True(registry.TryGet(id!, out var registered));
        Assert.Same(obj, registered);
    }

    [Fact]
    public void ResolveValue_ReturnsPrimitiveValues()
    {
        var registry = new ObjectRegistry();

        Assert.Equal("hello", registry.ResolveValue(JsonDocument.Parse("\"hello\"").RootElement));
        // JSON numbers parse as long if they fit
        var numResult = registry.ResolveValue(JsonDocument.Parse("42").RootElement);
        Assert.True(numResult is long or int or double);
        Assert.Equal(42L, Convert.ToInt64(numResult, System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(true, registry.ResolveValue(JsonDocument.Parse("true").RootElement));
        Assert.Equal(false, registry.ResolveValue(JsonDocument.Parse("false").RootElement));
        Assert.Null(registry.ResolveValue(JsonDocument.Parse("null").RootElement));
    }

    [Fact]
    public void ResolveValue_ResolvesProxyReference()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test" };
        var id = registry.Register(obj);

        var json = JsonDocument.Parse($"{{\"$id\": \"{id}\"}}").RootElement;
        var resolved = registry.ResolveValue(json);

        Assert.Same(obj, resolved);
    }

    [Fact]
    public void ResolveValueObject_ResolvesProxyFromDictionary()
    {
        var registry = new ObjectRegistry();
        var obj = new TestClass { Name = "test" };
        var id = registry.Register(obj);

        var dict = new Dictionary<string, object?> { ["$id"] = id };
        var resolved = registry.ResolveValueObject(dict);

        Assert.Same(obj, resolved);
    }

    [Fact]
    public void ResolveValueObject_ReturnsNullForNull()
    {
        var registry = new ObjectRegistry();

        var resolved = registry.ResolveValueObject(null);

        Assert.Null(resolved);
    }

    [Fact]
    public void ResolveValueObject_ReturnsValueForNonProxy()
    {
        var registry = new ObjectRegistry();
        var value = "just a string";

        var resolved = registry.ResolveValueObject(value);

        Assert.Equal(value, resolved);
    }

    private sealed class TestClass
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private sealed class TestClassWithNestedObject
    {
        public string? Name { get; set; }
        public TestClass? Nested { get; set; }
    }

    private enum TestEnum
    {
        Value1,
        Value2
    }
}
