// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class RpcOperationsTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public RpcOperationsTests()
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

    #region InvokeMethod Tests

    [Fact]
    public void InvokeMethod_CallsMethodOnRegisteredObject()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        _operations.InvokeMethod(id, "DoSomething", null);

        Assert.True(obj.WasCalled);
    }

    [Fact]
    public void InvokeMethod_PassesArguments()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonDocument.Parse("{\"value\": 42}").RootElement;

        _operations.InvokeMethod(id, "SetValue", args);

        Assert.Equal(42, obj.Value);
    }

    [Fact]
    public void InvokeMethod_ReturnsSimpleValue()
    {
        var obj = new TestObject { Value = 123 };
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetValue", null);

        Assert.Equal(123, result);
    }

    [Fact]
    public void InvokeMethod_MarshallesComplexReturnValue()
    {
        var obj = new TestObject { Name = "parent" };
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetSelf", null);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("TestObject", dict["$type"]);
        Assert.True(dict.ContainsKey("$id"));
    }

    [Fact]
    public void InvokeMethod_ThrowsForUnknownObject()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeMethod("unknown", "DoSomething", null));

        Assert.Contains("unknown", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeMethod_ThrowsForUnknownMethod()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeMethod(id, "NonExistentMethod", null));

        Assert.Contains("NonExistentMethod", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeMethod_UsesDefaultValueForOptionalParameter()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        _operations.InvokeMethod(id, "MethodWithOptional", null);

        Assert.Equal(100, obj.Value); // Default value
    }

    [Fact]
    public void InvokeMethod_ThrowsForMissingRequiredParameter()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeMethod(id, "SetValue", null));

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

        var result = _operations.GetProperty(id, "Name");

        Assert.Equal("test", result);
    }

    [Fact]
    public void GetProperty_MarshallesComplexValue()
    {
        var nested = new TestObject { Name = "nested" };
        var obj = new TestObjectWithNested { Nested = nested };
        var id = _objectRegistry.Register(obj);

        var result = _operations.GetProperty(id, "Nested");

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
            _operations.GetProperty(id, "NonExistentProperty"));

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

        _operations.SetProperty(id, "Name", value);

        Assert.Equal("new value", obj.Name);
    }

    [Fact]
    public void SetProperty_ThrowsForReadOnlyProperty()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var value = JsonDocument.Parse("42").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetProperty(id, "ReadOnlyValue", value));

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

        var result = _operations.GetIndexer(id, index);

        Assert.Equal("second", result);
    }

    [Fact]
    public void GetIndexer_ReturnsDictionaryItem()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10, ["key2"] = 20 };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"key2\"").RootElement;

        var result = _operations.GetIndexer(id, key);

        Assert.Equal(20, result);
    }

    [Fact]
    public void GetIndexer_MarshallesComplexListItem()
    {
        var list = new List<TestObject> { new() { Name = "item1" } };
        var id = _objectRegistry.Register(list);
        var index = JsonDocument.Parse("0").RootElement;

        var result = _operations.GetIndexer(id, index);

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
            _operations.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_ReturnsNullForMissingDictionaryKey()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10 };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"nonexistent\"").RootElement;

        var result = _operations.GetIndexer(id, key);

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

        _operations.SetIndexer(id, index, value);

        Assert.Equal("updated", list[0]);
    }

    [Fact]
    public void SetIndexer_SetsDictionaryItem()
    {
        var dict = new Dictionary<string, object?> { ["key1"] = "old" };
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"key1\"").RootElement;
        var value = JsonDocument.Parse("\"new\"").RootElement;

        _operations.SetIndexer(id, key, value);

        Assert.Equal("new", dict["key1"]);
    }

    [Fact]
    public void SetIndexer_AddsDictionaryItem()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);
        var key = JsonDocument.Parse("\"newkey\"").RootElement;
        var value = JsonDocument.Parse("\"newvalue\"").RootElement;

        _operations.SetIndexer(id, key, value);

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

        _operations.SetIndexer(dictId, key, value);

        Assert.Same(refObj, dict["mykey"]);
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

        var result = _operations.GetStaticProperty(assemblyName, typeName, "StaticValue");

        Assert.Equal("test-value", result);
    }

    [Fact]
    public void GetStaticProperty_ReturnsReadOnlyStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        var result = _operations.GetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue");

        Assert.Equal("readonly", result);
    }

    [Fact]
    public void GetStaticProperty_MarshallesComplexValue()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        // Reset to known state
        StaticPropertyTestClass.ComplexStatic = new TestObject { Name = "complex-static" };

        var result = _operations.GetStaticProperty(assemblyName, typeName, "ComplexStatic");

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
            _operations.GetStaticProperty(assemblyName, "NonExistent.Type.That.DoesNotExist", "SomeProperty"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetStaticProperty_ThrowsForUnknownProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.GetStaticProperty(assemblyName, typeName, "NonExistentProperty"));

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

        _operations.SetStaticProperty(assemblyName, typeName, "StaticValue", value);

        Assert.Equal("new-value", StaticPropertyTestClass.StaticValue);
    }

    [Fact]
    public void SetStaticProperty_SetsIntStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("42").RootElement;

        _operations.SetStaticProperty(assemblyName, typeName, "StaticInt", value);

        Assert.Equal(42, StaticPropertyTestClass.StaticInt);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForReadOnlyProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue", value));

        Assert.Contains("read-only", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownType()
    {
        // Use the same assembly but a non-existent type
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty(assemblyName, "NonExistent.Type.That.DoesNotExist", "SomeProperty", value));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonDocument.Parse("\"value\"").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty(assemblyName, typeName, "NonExistentProperty", value));

        Assert.Contains("NonExistentProperty", ex.Message);
        Assert.Contains("not found", ex.Message);
    }

    #endregion

    #region CreateObject RPC Method Tests

    [Fact]
    public void CreateObject_CreatesInstanceWithNoArgs()
    {
        var assemblyName = typeof(SimpleTestClass).Assembly.GetName().Name!;
        var typeName = typeof(SimpleTestClass).FullName!;

        var result = _operations.CreateObject(assemblyName, typeName, null);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("SimpleTestClass", dict["$type"]);
        Assert.True(dict.ContainsKey("$id"));
    }

    [Fact]
    public void CreateObject_CreatesInstanceWithConstructorArgs()
    {
        var assemblyName = typeof(TestClassWithArgs).Assembly.GetName().Name!;
        var typeName = typeof(TestClassWithArgs).FullName!;
        var args = JsonDocument.Parse("{\"name\": \"test-name\", \"value\": 42}").RootElement;

        var result = _operations.CreateObject(assemblyName, typeName, args);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        var objectId = (string)dict["$id"]!;

        // Verify properties were set correctly
        var nameValue = _operations.GetProperty(objectId, "Name");
        Assert.Equal("test-name", nameValue);

        var valueValue = _operations.GetProperty(objectId, "Value");
        Assert.Equal(42, valueValue);
    }

    [Fact]
    public void CreateObject_UsesDefaultValuesForOptionalParameters()
    {
        var assemblyName = typeof(TestClassWithOptionalArgs).Assembly.GetName().Name!;
        var typeName = typeof(TestClassWithOptionalArgs).FullName!;
        var args = JsonDocument.Parse("{\"name\": \"required-name\"}").RootElement;

        var result = _operations.CreateObject(assemblyName, typeName, args);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        var objectId = (string)dict["$id"]!;

        // Verify default value was used
        var valueValue = _operations.GetProperty(objectId, "Value");
        Assert.Equal(100, valueValue);
    }

    [Fact]
    public void CreateObject_ThrowsForUnknownAssembly()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.CreateObject("NonExistent.Assembly", "SomeType", null));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void CreateObject_ThrowsForUnknownType()
    {
        var assemblyName = typeof(SimpleTestClass).Assembly.GetName().Name!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.CreateObject(assemblyName, "NonExistent.Type", null));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void CreateObject_ThrowsForMissingRequiredArgs()
    {
        var assemblyName = typeof(TestClassWithArgs).Assembly.GetName().Name!;
        var typeName = typeof(TestClassWithArgs).FullName!;
        var args = JsonDocument.Parse("{}").RootElement;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.CreateObject(assemblyName, typeName, args));

        Assert.Contains("not provided", ex.Message);
    }

    #endregion

    #region InvokeStaticMethod Tests (Direct RPC Method)

    [Fact]
    public void InvokeStaticMethod_CallsStaticMethod()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticTestClass).FullName!;
        var args = JsonDocument.Parse("{\"a\": 10, \"b\": 5}").RootElement;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "Add", args);

        Assert.Equal(15, result);
    }

    [Fact]
    public void InvokeStaticMethod_MarshallesComplexReturnValue()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticTestClass).FullName!;
        var args = JsonDocument.Parse("{\"name\": \"test-object\"}").RootElement;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "CreateInstance", args);

        Assert.IsType<Dictionary<string, object?>>(result);
        var dict = (Dictionary<string, object?>)result!;
        Assert.Equal("StaticTestClass", dict["$type"]);
        Assert.True(dict.ContainsKey("$id"));
    }

    [Fact]
    public void InvokeStaticMethod_ResolvesObjectReferences()
    {
        // Register an object and pass it as an argument
        var existingObj = new TestObject { Value = 42 };
        var objId = _objectRegistry.Register(existingObj);

        var assemblyName = typeof(ExtensionMethodTestClass).Assembly.GetName().Name!;
        var typeName = typeof(ExtensionMethodTestClass).FullName!;
        var args = JsonDocument.Parse($"{{\"obj\": {{\"$id\": \"{objId}\"}}, \"amount\": 10}}").RootElement;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "IncrementValue", args);

        Assert.Equal(52, result);
        Assert.Equal(52, existingObj.Value);
    }

    [Fact]
    public void InvokeStaticMethod_HandlesGenericMethods()
    {
        // Register a generic IResourceBuilder<T>-like object
        var container = new GenericContainer { Name = "my-container" };
        var objId = _objectRegistry.Register(container);

        var assemblyName = typeof(ExtensionMethodTestClass).Assembly.GetName().Name!;
        var typeName = typeof(ExtensionMethodTestClass).FullName!;
        var args = JsonDocument.Parse($"{{\"builder\": {{\"$id\": \"{objId}\"}}, \"envName\": \"TEST_VAR\", \"envValue\": \"test-value\"}}").RootElement;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "WithEnvironment", args);

        // The method returns the same object
        Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Single(container.EnvironmentVariables);
        Assert.Equal("test-value", container.EnvironmentVariables["TEST_VAR"]);
    }

    [Fact]
    public void InvokeStaticMethod_ThrowsForUnknownAssembly()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeStaticMethod("NonExistent.Assembly", "SomeType", "SomeMethod", null));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeStaticMethod_ThrowsForUnknownType()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeStaticMethod(assemblyName, "NonExistent.Type", "SomeMethod", null));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeStaticMethod_ThrowsForUnknownMethod()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticTestClass).FullName!;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeStaticMethod(assemblyName, typeName, "NonExistentMethod", null));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeStaticMethod_ResolvesOverloadByArgumentNames()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticTestClass).FullName!;

        // Single arg - should use Format(string value)
        var args1 = JsonDocument.Parse("{\"value\": \"hello\"}").RootElement;
        var result1 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args1);
        Assert.Equal("[hello]", result1);

        // Two args with count - should use Format(string value, int count)
        var args2 = JsonDocument.Parse("{\"value\": \"x\", \"count\": 2}").RootElement;
        var result2 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args2);
        Assert.Equal("[x][x]", result2);

        // Three string args - should use Format(string value, string prefix, string suffix)
        var args3 = JsonDocument.Parse("{\"value\": \"test\", \"prefix\": \"<\", \"suffix\": \">\"}").RootElement;
        var result3 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args3);
        Assert.Equal("<test>", result3);
    }

    #endregion

    #region Test Classes

    // Classes for CreateObject tests
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
    /// Test class with static methods for testing InvokeStaticMethod.
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

    /// <summary>
    /// Test class simulating extension methods for testing InvokeStaticMethod with object references.
    /// </summary>
    public static class ExtensionMethodTestClass
    {
        // Simulates an extension method like: obj.IncrementValue(10)
        public static int IncrementValue(TestObject obj, int amount)
        {
            obj.Value += amount;
            return obj.Value;
        }

        // Simulates a generic extension method like: builder.WithEnvironment("KEY", "VALUE")
        public static IGenericBuilder<T> WithEnvironment<T>(IGenericBuilder<T> builder, string envName, string envValue)
            where T : class
        {
            builder.EnvironmentVariables[envName] = envValue;
            return builder;
        }
    }

    /// <summary>
    /// Interface simulating IResourceBuilder for generic method tests.
    /// </summary>
    public interface IGenericBuilder<T> where T : class
    {
        string Name { get; }
        Dictionary<string, string> EnvironmentVariables { get; }
    }

    /// <summary>
    /// Concrete implementation for testing generic extension methods.
    /// </summary>
    public sealed class GenericContainer : IGenericBuilder<GenericContainer>
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> EnvironmentVariables { get; } = new();
    }

    #endregion
}
