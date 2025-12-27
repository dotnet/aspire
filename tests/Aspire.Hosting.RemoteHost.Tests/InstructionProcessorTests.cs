// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class InstructionProcessorTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly InstructionProcessor _processor;

    public InstructionProcessorTests()
    {
        _objectRegistry = new ObjectRegistry();
        _callbackInvoker = new TestCallbackInvoker();
        _processor = new InstructionProcessor(_objectRegistry, _callbackInvoker);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
    }

    #region InvokeMethod Tests

    [Fact]
    public void InvokeMethod_CallsMethodOnRegisteredObject()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        _processor.InvokeMethod(id, "DoSomething", null);

        Assert.True(obj.WasCalled);
    }

    [Fact]
    public void InvokeMethod_PassesArguments()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonDocument.Parse("{\"value\": 42}").RootElement;

        _processor.InvokeMethod(id, "SetValue", args);

        Assert.Equal(42, obj.Value);
    }

    [Fact]
    public void InvokeMethod_ReturnsSimpleValue()
    {
        var obj = new TestObject { Value = 123 };
        var id = _objectRegistry.Register(obj);

        var result = _processor.InvokeMethod(id, "GetValue", null);

        Assert.Equal(123, result);
    }

    [Fact]
    public void InvokeMethod_MarshallesComplexReturnValue()
    {
        var obj = new TestObject { Name = "parent" };
        var id = _objectRegistry.Register(obj);

        var result = _processor.InvokeMethod(id, "GetSelf", null);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("TestObject", dict["$type"]);
        Assert.True(dict.ContainsKey("$id"));
    }

    [Fact]
    public void InvokeMethod_ThrowsForUnknownObject()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.InvokeMethod("unknown", "DoSomething", null));

        Assert.Contains("unknown", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeMethod_ThrowsForUnknownMethod()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.InvokeMethod(id, "NonExistentMethod", null));

        Assert.Contains("NonExistentMethod", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeMethod_UsesDefaultValueForOptionalParameter()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        _processor.InvokeMethod(id, "MethodWithOptional", null);

        Assert.Equal(100, obj.Value); // Default value
    }

    [Fact]
    public void InvokeMethod_ThrowsForMissingRequiredParameter()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.InvokeMethod(id, "SetValue", null));

        Assert.Contains("value", ex.Message);
        Assert.Contains("not provided", ex.Message);
    }

    #endregion

    #region GetProperty Tests

    [Fact]
    public void GetProperty_ReturnsPropertyValue()
    {
        var obj = new TestObject { Name = "test" };
        var id = _objectRegistry.Register(obj);

        var result = _processor.GetProperty(id, "Name");

        Assert.Equal("test", result);
    }

    [Fact]
    public void GetProperty_MarshallesComplexValue()
    {
        var nested = new TestObject { Name = "nested" };
        var obj = new TestObjectWithNested { Nested = nested };
        var id = _objectRegistry.Register(obj);

        var result = _processor.GetProperty(id, "Nested");

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("TestObject", dict["$type"]);
    }

    [Fact]
    public void GetProperty_ThrowsForUnknownProperty()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.GetProperty(id, "NonExistentProperty"));

        Assert.Contains("NonExistentProperty", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region SetProperty Tests

    [Fact]
    public void SetProperty_SetsPropertyValue()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var value = JsonDocument.Parse("\"new value\"").RootElement;

        _processor.SetProperty(id, "Name", value);

        Assert.Equal("new value", obj.Name);
    }

    [Fact]
    public void SetProperty_ThrowsForReadOnlyProperty()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var value = JsonDocument.Parse("42").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.SetProperty(id, "ReadOnlyValue", value));

        Assert.Contains("read-only", ex.Message);
    }

    #endregion

    #region GetIndexer Tests

    [Fact]
    public void GetIndexer_ReturnsListItem()
    {
        var list = new List<string> { "first", "second", "third" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("1").RootElement;

        var result = _processor.GetIndexer(id, index);

        Assert.Equal("second", result);
    }

    [Fact]
    public void GetIndexer_ReturnsDictionaryItem()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10, ["key2"] = 20 };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"key2\"").RootElement;

        var result = _processor.GetIndexer(id, key);

        Assert.Equal(20, result);
    }

    [Fact]
    public void GetIndexer_MarshallesComplexListItem()
    {
        var list = new List<TestObject> { new() { Name = "item1" } };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("0").RootElement;

        var result = _processor.GetIndexer(id, index);

        Assert.IsType<Dictionary<string, object?>>(result);
        var marshalledItem = (Dictionary<string, object?>)result!;
        Assert.Equal("TestObject", marshalledItem["$type"]);
    }

    [Fact]
    public void GetIndexer_ThrowsForOutOfRangeIndex()
    {
        var list = new List<string> { "only" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("5").RootElement;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _processor.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_ReturnsNullForMissingDictionaryKey()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10 };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"nonexistent\"").RootElement;

        var result = _processor.GetIndexer(id, key);

        Assert.Null(result);
    }

    #endregion

    #region SetIndexer Tests

    [Fact]
    public void SetIndexer_SetsListItem()
    {
        var list = new List<string> { "first", "second" };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("0").RootElement;
        var value = JsonDocument.Parse("\"updated\"").RootElement;

        _processor.SetIndexer(id, index, value);

        Assert.Equal("updated", list[0]);
    }

    [Fact]
    public void SetIndexer_SetsDictionaryItem()
    {
        var dict = new Dictionary<string, object?> { ["key1"] = "old" };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"key1\"").RootElement;
        var value = JsonDocument.Parse("\"new\"").RootElement;

        _processor.SetIndexer(id, key, value);

        Assert.Equal("new", dict["key1"]);
    }

    [Fact]
    public void SetIndexer_AddsDictionaryItem()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"newkey\"").RootElement;
        var value = JsonDocument.Parse("\"newvalue\"").RootElement;

        _processor.SetIndexer(id, key, value);

        Assert.Equal("newvalue", dict["newkey"]);
    }

    [Fact]
    public void SetIndexer_ResolvesProxyReference()
    {
        var dict = new Dictionary<string, object?>();
        var dictId = _objectRegistry.Register(dict);

        var refObj = new TestObject { Name = "referenced" };
        var refId = _objectRegistry.Register(refObj);

        var key = JsonDocument.Parse("\"mykey\"").RootElement;
        var value = JsonDocument.Parse($"{{\"$id\": \"{refId}\"}}").RootElement;

        _processor.SetIndexer(dictId, key, value);

        Assert.Same(refObj, dict["mykey"]);
    }

    #endregion

    #region Callback Tests

    [Fact]
    public async Task InvokeCallbackAsync_CallsCallbackInvoker()
    {
        await _processor.InvokeCallbackAsync("test_callback", "arg");

        Assert.Single(_callbackInvoker.Invocations);
        var (callbackId, args) = _callbackInvoker.Invocations[0];
        Assert.Equal("test_callback", callbackId);
        Assert.Equal("arg", args);
    }

    [Fact]
    public async Task InvokeCallbackAsync_MarshallesComplexArgs()
    {
        var complexArg = new TestObject { Name = "complex" };

        await _processor.InvokeCallbackAsync("test_callback", complexArg);

        Assert.Single(_callbackInvoker.Invocations);
        var (_, args) = _callbackInvoker.Invocations[0];
        Assert.IsType<Dictionary<string, object?>>(args);
        var dict = (Dictionary<string, object?>)args!;
        Assert.Equal("TestObject", dict["$type"]);
    }

    [Fact]
    public async Task InvokeCallbackAsync_ReturnsResult()
    {
        _callbackInvoker.RegisterHandler("test_callback", 42);

        var result = await _processor.InvokeCallbackAsync<int>("test_callback", null);

        Assert.Equal(42, result);
    }

    #endregion

    #region Test Classes

    private sealed class TestObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public bool WasCalled { get; private set; }
        public int ReadOnlyValue => Value + 42;

        public void DoSomething()
        {
            WasCalled = true;
        }

        public void SetValue(int value)
        {
            Value = value;
        }

        public int GetValue()
        {
            return Value;
        }

        public TestObject GetSelf()
        {
            return this;
        }

        public void MethodWithOptional(int value = 100)
        {
            Value = value;
        }
    }

    private sealed class TestObjectWithNested
    {
        public TestObject? Nested { get; set; }
    }

    #endregion
}
