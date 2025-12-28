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

    #region ExecuteInstruction Tests (CREATE_OBJECT)

    [Fact]
    public async Task CreateObject_CreatesInstanceWithNoArgs()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "Aspire.Hosting.RemoteHost.Tests.InstructionProcessorTests+SimpleTestClass, Aspire.Hosting.RemoteHost.Tests",
                "target": "obj1"
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var createResult = Assert.IsType<CreateObjectResult>(result);
        Assert.True(createResult.Success);
        Assert.Equal("obj1", createResult.Target);
    }

    [Fact]
    public async Task CreateObject_CreatesInstanceWithConstructorArgs()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "Aspire.Hosting.RemoteHost.Tests.InstructionProcessorTests+TestClassWithArgs, Aspire.Hosting.RemoteHost.Tests",
                "target": "obj1",
                "args": {
                    "name": "test-name",
                    "value": 42
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var createResult = Assert.IsType<CreateObjectResult>(result);
        Assert.True(createResult.Success);

        // Verify the object was created with correct args by accessing its properties
        var marshalledResult = (Dictionary<string, object?>)createResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;

        var nameValue = _processor.GetProperty(objectId, "Name");
        Assert.Equal("test-name", nameValue);

        var valueValue = _processor.GetProperty(objectId, "Value");
        Assert.Equal(42, valueValue);
    }

    [Fact]
    public async Task CreateObject_UsesDefaultValuesForOptionalParameters()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "Aspire.Hosting.RemoteHost.Tests.InstructionProcessorTests+TestClassWithOptionalArgs, Aspire.Hosting.RemoteHost.Tests",
                "target": "obj1",
                "args": {
                    "name": "required-name"
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var createResult = Assert.IsType<CreateObjectResult>(result);
        Assert.True(createResult.Success);

        // Verify the optional value got its default
        var marshalledResult = (Dictionary<string, object?>)createResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;

        var valueValue = _processor.GetProperty(objectId, "Value");
        Assert.Equal(100, valueValue); // Default value
    }

    [Fact]
    public async Task CreateObject_ThrowsForUnknownType()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "NonExistent.Type.That.DoesNotExist",
                "target": "obj1"
            }
            """;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.ExecuteInstructionAsync(instruction));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task CreateObject_ThrowsForMissingRequiredArgs()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "Aspire.Hosting.RemoteHost.Tests.InstructionProcessorTests+TestClassWithArgs, Aspire.Hosting.RemoteHost.Tests",
                "target": "obj1",
                "args": {}
            }
            """;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.ExecuteInstructionAsync(instruction));

        Assert.Contains("not provided", ex.Message);
    }

    [Fact]
    public async Task CreateObject_RegistersObjectForLaterUse()
    {
        var instruction = """
            {
                "name": "CREATE_OBJECT",
                "typeName": "Aspire.Hosting.RemoteHost.Tests.InstructionProcessorTests+SimpleTestClass, Aspire.Hosting.RemoteHost.Tests",
                "target": "myObject"
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var createResult = Assert.IsType<CreateObjectResult>(result);
        var marshalledResult = (Dictionary<string, object?>)createResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;

        // Verify the object is in the registry
        var registeredObj = _objectRegistry.Get(objectId);
        Assert.NotNull(registeredObj);
        Assert.IsType<SimpleTestClass>(registeredObj);
    }

    [Fact]
    public async Task UnsupportedInstruction_Throws()
    {
        var instruction = """
            {
                "name": "UNKNOWN_INSTRUCTION"
            }
            """;

        var ex = await Assert.ThrowsAsync<NotSupportedException>(() =>
            _processor.ExecuteInstructionAsync(instruction));

        Assert.Contains("UNKNOWN_INSTRUCTION", ex.Message);
        Assert.Contains("not supported", ex.Message);
    }

    #endregion

    #region INVOKE Static Method Tests

    [Fact]
    public async Task Invoke_StaticMethod_CallsMethodWithoutSource()
    {
        // Call a static method without a source object
        var instruction = $$"""
            {
                "name": "INVOKE",
                "source": "",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "CreateInstance",
                "args": {
                    "name": "test-name"
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var invokeResult = Assert.IsType<InvokeResult>(result);
        Assert.True(invokeResult.Success);
        Assert.True(string.IsNullOrEmpty(invokeResult.Source)); // Static method - no source
        Assert.Equal("result", invokeResult.Target);

        // Verify the result was registered and is correct
        var marshalledResult = (Dictionary<string, object?>)invokeResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;

        var nameValue = _processor.GetProperty(objectId, "Name");
        Assert.Equal("test-name", nameValue);
    }

    [Fact]
    public async Task Invoke_StaticMethod_WithNullSource()
    {
        // Call a static method with null source (same as empty)
        var instruction = $$"""
            {
                "name": "INVOKE",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "Add",
                "args": {
                    "a": 5,
                    "b": 3
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var invokeResult = Assert.IsType<InvokeResult>(result);
        Assert.True(invokeResult.Success);
        // Primitives like int are marshalled too
        var marshalledResult = (Dictionary<string, object?>)invokeResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;
        var actualValue = _objectRegistry.Get(objectId);
        Assert.Equal(8, actualValue);
    }

    [Fact]
    public async Task Invoke_StaticMethod_ThrowsForUnknownMethod()
    {
        var instruction = $$"""
            {
                "name": "INVOKE",
                "source": "",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "NonExistentMethod",
                "args": {}
            }
            """;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _processor.ExecuteInstructionAsync(instruction));

        Assert.Contains("NonExistentMethod", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public async Task Invoke_StaticMethod_ResolvesOverloadByArgumentNames_SingleArg()
    {
        // Should resolve to Format(string value) - one argument
        var instruction = $$"""
            {
                "name": "INVOKE",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "Format",
                "args": {
                    "value": "test"
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var invokeResult = Assert.IsType<InvokeResult>(result);
        Assert.True(invokeResult.Success);
        // Result is marshalled - for strings, get the object and verify via registry
        var marshalledResult = (Dictionary<string, object?>)invokeResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;
        var actualValue = _objectRegistry.Get(objectId);
        Assert.Equal("[test]", actualValue);
    }

    [Fact]
    public async Task Invoke_StaticMethod_ResolvesOverloadByArgumentNames_TwoArgs()
    {
        // Should resolve to Format(string value, int count) - two arguments
        var instruction = $$"""
            {
                "name": "INVOKE",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "Format",
                "args": {
                    "value": "x",
                    "count": 3
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var invokeResult = Assert.IsType<InvokeResult>(result);
        Assert.True(invokeResult.Success);
        var marshalledResult = (Dictionary<string, object?>)invokeResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;
        var actualValue = _objectRegistry.Get(objectId);
        Assert.Equal("[x][x][x]", actualValue);
    }

    [Fact]
    public async Task Invoke_StaticMethod_ResolvesOverloadByArgumentNames_ThreeArgs()
    {
        // Should resolve to Format(string value, string prefix, string suffix)
        var instruction = $$"""
            {
                "name": "INVOKE",
                "target": "result",
                "methodAssembly": "{{typeof(StaticTestClass).Assembly.GetName().Name}}",
                "methodType": "{{typeof(StaticTestClass).FullName}}",
                "methodName": "Format",
                "args": {
                    "value": "hello",
                    "prefix": "<<",
                    "suffix": ">>"
                }
            }
            """;

        var result = await _processor.ExecuteInstructionAsync(instruction);

        var invokeResult = Assert.IsType<InvokeResult>(result);
        Assert.True(invokeResult.Success);
        var marshalledResult = (Dictionary<string, object?>)invokeResult.Result!;
        var objectId = (string)marshalledResult["$id"]!;
        var actualValue = _objectRegistry.Get(objectId);
        Assert.Equal("<<hello>>", actualValue);
    }

    #endregion

    #region GetStaticProperty Tests

    [Fact]
    public void GetStaticProperty_ReturnsStaticPropertyValue()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        // Reset to known state
        StaticPropertyTestClass.StaticValue = "test-value";

        var result = _processor.GetStaticProperty(assemblyName, typeName, "StaticValue");

        Assert.Equal("test-value", result);
    }

    [Fact]
    public void GetStaticProperty_ReturnsReadOnlyStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        var result = _processor.GetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue");

        Assert.Equal("readonly", result);
    }

    [Fact]
    public void GetStaticProperty_MarshallesComplexValue()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        // Reset to known state
        StaticPropertyTestClass.ComplexStatic = new TestObject { Name = "complex-static" };

        var result = _processor.GetStaticProperty(assemblyName, typeName, "ComplexStatic");

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("TestObject", dict["$type"]);
    }

    [Fact]
    public void GetStaticProperty_ThrowsForUnknownType()
    {
        // Use the same assembly but a non-existent type
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.GetStaticProperty(assemblyName, "NonExistent.Type.That.DoesNotExist", "SomeProperty"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetStaticProperty_ThrowsForUnknownProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.GetStaticProperty(assemblyName, typeName, "NonExistentProperty"));

        Assert.Contains("NonExistentProperty", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region SetStaticProperty Tests

    [Fact]
    public void SetStaticProperty_SetsStaticPropertyValue()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("\"new-value\"").RootElement;

        _processor.SetStaticProperty(assemblyName, typeName, "StaticValue", value);

        Assert.Equal("new-value", StaticPropertyTestClass.StaticValue);
    }

    [Fact]
    public void SetStaticProperty_SetsIntStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("42").RootElement;

        _processor.SetStaticProperty(assemblyName, typeName, "StaticInt", value);

        Assert.Equal(42, StaticPropertyTestClass.StaticInt);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForReadOnlyProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.SetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue", value));

        Assert.Contains("read-only", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownType()
    {
        // Use the same assembly but a non-existent type
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.SetStaticProperty(assemblyName, "NonExistent.Type.That.DoesNotExist", "SomeProperty", value));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _processor.SetStaticProperty(assemblyName, typeName, "NonExistentProperty", value));

        Assert.Contains("NonExistentProperty", ex.Message);
        Assert.Contains("not found", ex.Message);
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

    // Classes for CREATE_OBJECT tests
    public sealed class SimpleTestClass
    {
        public string Name { get; set; } = "default";
    }

    public sealed class TestClassWithArgs
    {
        public string Name { get; }
        public int Value { get; }

        public TestClassWithArgs(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class TestClassWithOptionalArgs
    {
        public string Name { get; }
        public int Value { get; }

        public TestClassWithOptionalArgs(string name, int value = 100)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>
    /// Test class with static methods for testing INVOKE without source.
    /// </summary>
    public sealed class StaticTestClass
    {
        public string Name { get; }

        private StaticTestClass(string name)
        {
            Name = name;
        }

        // Static factory method
        public static StaticTestClass CreateInstance(string name)
        {
            return new StaticTestClass(name);
        }

        // Simple static method returning primitive
        public static int Add(int a, int b)
        {
            return a + b;
        }

        // Overloaded static methods
        public static string Format(string value)
        {
            return $"[{value}]";
        }

        public static string Format(string value, int count)
        {
            return string.Concat(Enumerable.Repeat($"[{value}]", count));
        }

        public static string Format(string value, string prefix, string suffix)
        {
            return $"{prefix}{value}{suffix}";
        }
    }

    public sealed class TestObject
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

    /// <summary>
    /// Test class with static properties for testing GetStaticProperty/SetStaticProperty.
    /// </summary>
    public sealed class StaticPropertyTestClass
    {
        public static string? StaticValue { get; set; } = "default";
        public static int StaticInt { get; set; }
        public static string ReadOnlyStaticValue { get; } = "readonly";
        public static TestObject? ComplexStatic { get; set; }
    }

    #endregion
}
