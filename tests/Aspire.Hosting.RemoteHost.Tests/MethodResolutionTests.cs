// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class MethodResolutionTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public MethodResolutionTests()
    {
        _objectRegistry = new ObjectRegistry();
        _callbackInvoker = new TestCallbackInvoker();
        _operations = new RpcOperations(_objectRegistry, _callbackInvoker);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await _operations.DisposeAsync();
    }

    #region Overload Resolution Tests

    [Fact]
    public void InvokeMethod_SelectsOverloadByArgumentNames()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call with 'name' argument - should select Method(string name)
        var args1 = JsonNode.Parse("{\"name\": \"test\"}") as JsonObject;
        _operations.InvokeMethod(id, "Method", args1);
        Assert.Equal("name:test", obj.LastCall);

        // Call with 'value' argument - should select Method(int value)
        var args2 = JsonNode.Parse("{\"value\": 42}") as JsonObject;
        _operations.InvokeMethod(id, "Method", args2);
        Assert.Equal("value:42", obj.LastCall);
    }

    [Fact]
    public void InvokeMethod_SelectsOverloadWithMostMatchingArgs()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call with both 'name' and 'value' - should select Method(string name, int value)
        var args = JsonNode.Parse("{\"name\": \"test\", \"value\": 42}") as JsonObject;
        _operations.InvokeMethod(id, "Method", args);
        Assert.Equal("name:test,value:42", obj.LastCall);
    }

    [Fact]
    public void InvokeMethod_PrefersMethodsWithoutMissingRequiredArgs()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call with only 'name' - should prefer Method(string name) over Method(string name, int value)
        // because the latter has a required 'value' parameter
        var args = JsonNode.Parse("{\"name\": \"test\"}") as JsonObject;
        _operations.InvokeMethod(id, "Method", args);
        Assert.Equal("name:test", obj.LastCall);
    }

    [Fact]
    public void InvokeMethod_UsesDefaultValuesForOptionalParams()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call MethodWithOptional with only required arg
        var args = JsonNode.Parse("{\"required\": \"hello\"}") as JsonObject;
        _operations.InvokeMethod(id, "MethodWithOptional", args);
        Assert.Equal("required:hello,optional:default_value", obj.LastCall);
    }

    [Fact]
    public void InvokeMethod_OverridesDefaultValues()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call MethodWithOptional with both args
        var args = JsonNode.Parse("{\"required\": \"hello\", \"optional\": \"custom\"}") as JsonObject;
        _operations.InvokeMethod(id, "MethodWithOptional", args);
        Assert.Equal("required:hello,optional:custom", obj.LastCall);
    }

    [Fact]
    public void InvokeMethod_CaseInsensitiveMethodName()
    {
        var obj = new OverloadedMethods();
        var id = _objectRegistry.Register(obj);

        // Call with different casing
        var args = JsonNode.Parse("{\"name\": \"test\"}") as JsonObject;

        _operations.InvokeMethod(id, "METHOD", args);
        Assert.Equal("name:test", obj.LastCall);

        _operations.InvokeMethod(id, "method", args);
        Assert.Equal("name:test", obj.LastCall);
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void InvokeMethod_ConvertsJsonToInt()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": 42}") as JsonObject;
        _operations.InvokeMethod(id, "TakeInt", args);
        Assert.Equal(42, obj.IntValue);
    }

    [Fact]
    public void InvokeMethod_ConvertsJsonToLong()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": 9999999999}") as JsonObject;
        _operations.InvokeMethod(id, "TakeLong", args);
        Assert.Equal(9999999999L, obj.LongValue);
    }

    [Fact]
    public void InvokeMethod_ConvertsJsonToDouble()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": 3.14}") as JsonObject;
        _operations.InvokeMethod(id, "TakeDouble", args);
        Assert.Equal(3.14, obj.DoubleValue, precision: 2);
    }

    [Fact]
    public void InvokeMethod_ConvertsJsonToBool()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": true}") as JsonObject;
        _operations.InvokeMethod(id, "TakeBool", args);
        Assert.True(obj.BoolValue);
    }

    [Fact]
    public void InvokeMethod_ConvertsJsonToString()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": \"hello world\"}") as JsonObject;
        _operations.InvokeMethod(id, "TakeString", args);
        Assert.Equal("hello world", obj.StringValue);
    }

    [Fact]
    public void InvokeMethod_HandlesNullableWithValue()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": 42}") as JsonObject;
        _operations.InvokeMethod(id, "TakeNullableInt", args);
        Assert.Equal(42, obj.NullableIntValue);
    }

    [Fact]
    public void InvokeMethod_HandlesNullableWithNull()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);
        obj.NullableIntValue = 999; // Set initial value

        var args = JsonNode.Parse("{\"value\": null}") as JsonObject;
        _operations.InvokeMethod(id, "TakeNullableInt", args);
        Assert.Null(obj.NullableIntValue);
    }

    [Fact]
    public void InvokeMethod_ResolvesProxyReference()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        // Register another object and get its ID
        var refObj = new ReferencedObject { Data = "referenced_data" };
        var refId = _objectRegistry.Register(refObj);

        // Pass the proxy reference as an argument
        var args = JsonNode.Parse($"{{\"value\": {{\"$id\": \"{refId}\"}}}}") as JsonObject;
        _operations.InvokeMethod(id, "TakeObject", args);

        Assert.Same(refObj, obj.ObjectValue);
    }

    #endregion

    #region Complex Object Tests

    [Fact]
    public void InvokeMethod_DeserializesComplexObject()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"value\": {\"Name\": \"test\", \"Count\": 5}}") as JsonObject;
        _operations.InvokeMethod(id, "TakeComplexArg", args);

        Assert.NotNull(obj.ComplexValue);
        Assert.Equal("test", obj.ComplexValue!.Name);
        Assert.Equal(5, obj.ComplexValue.Count);
    }

    [Fact]
    public void InvokeMethod_DeserializesArray()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"values\": [1, 2, 3, 4, 5]}") as JsonObject;
        _operations.InvokeMethod(id, "TakeIntArray", args);

        Assert.NotNull(obj.IntArrayValue);
        Assert.Equal([1, 2, 3, 4, 5], obj.IntArrayValue);
    }

    [Fact]
    public void InvokeMethod_DeserializesStringArray()
    {
        var obj = new TypeConversionMethods();
        var id = _objectRegistry.Register(obj);

        var args = JsonNode.Parse("{\"values\": [\"a\", \"b\", \"c\"]}") as JsonObject;
        _operations.InvokeMethod(id, "TakeStringArray", args);

        Assert.NotNull(obj.StringArrayValue);
        Assert.Equal(["a", "b", "c"], obj.StringArrayValue);
    }

    #endregion

    #region Test Classes

    private sealed class OverloadedMethods
    {
        public string? LastCall { get; private set; }

        public void Method(string name)
        {
            LastCall = $"name:{name}";
        }

        public void Method(int value)
        {
            LastCall = $"value:{value}";
        }

        public void Method(string name, int value)
        {
            LastCall = $"name:{name},value:{value}";
        }

        public void MethodWithOptional(string required, string optional = "default_value")
        {
            LastCall = $"required:{required},optional:{optional}";
        }
    }

    private sealed class TypeConversionMethods
    {
        public int IntValue { get; private set; }
        public long LongValue { get; private set; }
        public double DoubleValue { get; private set; }
        public bool BoolValue { get; private set; }
        public string? StringValue { get; private set; }
        public int? NullableIntValue { get; set; }
        public object? ObjectValue { get; private set; }
        public ComplexArg? ComplexValue { get; private set; }
        public int[]? IntArrayValue { get; private set; }
        public string[]? StringArrayValue { get; private set; }

        public void TakeInt(int value) => IntValue = value;
        public void TakeLong(long value) => LongValue = value;
        public void TakeDouble(double value) => DoubleValue = value;
        public void TakeBool(bool value) => BoolValue = value;
        public void TakeString(string value) => StringValue = value;
        public void TakeNullableInt(int? value) => NullableIntValue = value;
        public void TakeObject(ReferencedObject value) => ObjectValue = value;
        public void TakeComplexArg(ComplexArg value) => ComplexValue = value;
        public void TakeIntArray(int[] values) => IntArrayValue = values;
        public void TakeStringArray(string[] values) => StringArrayValue = values;
    }

    private sealed class ReferencedObject
    {
        public string? Data { get; set; }
    }

    private sealed class ComplexArg
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    #endregion
}
