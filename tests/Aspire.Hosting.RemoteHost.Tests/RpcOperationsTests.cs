// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
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
        var args = JsonNode.Parse("{\"value\": 42}") as JsonObject;

        _operations.InvokeMethod(id, "SetValue", args);

        Assert.Equal(42, obj.Value);
    }

    [Fact]
    public void InvokeMethod_ReturnsSimpleValue()
    {
        var obj = new TestObject { Value = 123 };
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetValue", null);

        Assert.Equal(123, (result as JsonValue)?.GetValue<int>());
    }

    [Fact]
    public void InvokeMethod_MarshallesComplexReturnValue()
    {
        var obj = new TestObject { Name = "parent" };
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetSelf", null);

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("TestObject", jsonObj["$type"]!.GetValue<string>());
        Assert.True(jsonObj.ContainsKey("$id"));
    }

    [Fact]
    public void InvokeMethod_ReturnsPrimitiveArrayAsJsonArray()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetIntArray", null);

        var jsonArray = Assert.IsType<JsonArray>(result);
        Assert.Equal(5, jsonArray.Count);
        Assert.Equal(1, jsonArray[0]!.GetValue<int>());
        Assert.Equal(2, jsonArray[1]!.GetValue<int>());
        Assert.Equal(5, jsonArray[4]!.GetValue<int>());
    }

    [Fact]
    public void InvokeMethod_ReturnsStringArrayAsJsonArray()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetStringArray", null);

        var jsonArray = Assert.IsType<JsonArray>(result);
        Assert.Equal(3, jsonArray.Count);
        Assert.Equal("alpha", jsonArray[0]!.GetValue<string>());
        Assert.Equal("beta", jsonArray[1]!.GetValue<string>());
        Assert.Equal("gamma", jsonArray[2]!.GetValue<string>());
    }

    [Fact]
    public void InvokeMethod_ReturnsPrimitiveListAsJsonArray()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetIntList", null);

        var jsonArray = Assert.IsType<JsonArray>(result);
        Assert.Equal(3, jsonArray.Count);
        Assert.Equal(10, jsonArray[0]!.GetValue<int>());
        Assert.Equal(20, jsonArray[1]!.GetValue<int>());
        Assert.Equal(30, jsonArray[2]!.GetValue<int>());
    }

    [Fact]
    public void InvokeMethod_ReturnsPrimitiveDictionaryAsJsonObject()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetStringIntDictionary", null);

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.False(jsonObj.ContainsKey("$id")); // Not a marshalled object
        Assert.False(jsonObj.ContainsKey("$type"));
        Assert.Equal(3, jsonObj.Count);
        Assert.Equal(1, jsonObj["one"]!.GetValue<int>());
        Assert.Equal(2, jsonObj["two"]!.GetValue<int>());
        Assert.Equal(3, jsonObj["three"]!.GetValue<int>());
    }

    [Fact]
    public void InvokeMethod_ReturnsStringDictionaryAsJsonObject()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetStringStringDictionary", null);

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.False(jsonObj.ContainsKey("$id")); // Not a marshalled object
        Assert.Equal(2, jsonObj.Count);
        Assert.Equal("value1", jsonObj["key1"]!.GetValue<string>());
        Assert.Equal("value2", jsonObj["key2"]!.GetValue<string>());
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

    [Fact]
    public void InvokeMethod_ReturnsByteArrayAsBase64()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetByteArray", null);

        var jsonValue = result as JsonValue;
        Assert.NotNull(jsonValue);
        var base64 = jsonValue.GetValue<string>();
        var decoded = Convert.FromBase64String(base64);
        Assert.Equal(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }, decoded);
    }

    [Fact]
    public void InvokeMethod_AcceptsByteArrayAsBase64()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var base64 = Convert.ToBase64String(data);
        var args = JsonNode.Parse($"{{\"data\": \"{base64}\"}}") as JsonObject;

        _operations.InvokeMethod(id, "SetByteArray", args);

        Assert.Equal(data, obj.LastByteArray);
    }

    [Fact]
    public void InvokeMethod_ReturnsFloatAsJsonValue()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetFloat", null);

        Assert.Equal(3.14f, (result as JsonValue)?.GetValue<float>());
    }

    [Fact]
    public void InvokeMethod_ReturnsDecimalAsJsonValue()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetDecimal", null);

        Assert.Equal(123.456m, (result as JsonValue)?.GetValue<decimal>());
    }

    [Fact]
    public void InvokeMethod_ReturnsGuidAsJsonValue()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetGuid", null);

        Assert.Equal(new Guid("12345678-1234-1234-1234-123456789012"), (result as JsonValue)?.GetValue<Guid>());
    }

    [Fact]
    public void InvokeMethod_AcceptsFloatArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": 2.5}") as JsonObject;

        _operations.InvokeMethod(id, "SetFloat", args);

        Assert.Equal(2.5f, obj.LastFloat);
    }

    [Fact]
    public void InvokeMethod_AcceptsDecimalArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": 99.99}") as JsonObject;

        _operations.InvokeMethod(id, "SetDecimal", args);

        Assert.Equal(99.99m, obj.LastDecimal);
    }

    [Fact]
    public void InvokeMethod_AcceptsGuidArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": \"11111111-2222-3333-4444-555555555555\"}") as JsonObject;

        _operations.InvokeMethod(id, "SetGuid", args);

        Assert.Equal(new Guid("11111111-2222-3333-4444-555555555555"), obj.LastGuid);
    }

    [Fact]
    public void InvokeMethod_AcceptsUriArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": \"https://test.com/api\"}") as JsonObject;

        _operations.InvokeMethod(id, "SetUri", args);

        Assert.Equal(new Uri("https://test.com/api"), obj.LastUri);
    }

    [Fact]
    public void InvokeMethod_AcceptsDateOnlyArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": \"2025-12-25\"}") as JsonObject;

        _operations.InvokeMethod(id, "SetDateOnly", args);

        Assert.Equal(new DateOnly(2025, 12, 25), obj.LastDateOnly);
    }

    [Fact]
    public void InvokeMethod_AcceptsTimeOnlyArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": \"09:30:15\"}") as JsonObject;

        _operations.InvokeMethod(id, "SetTimeOnly", args);

        Assert.Equal(new TimeOnly(9, 30, 15), obj.LastTimeOnly);
    }

    [Fact]
    public void InvokeMethod_AcceptsEnumAsStringArgument()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"value\": \"Friday\"}") as JsonObject;

        _operations.InvokeMethod(id, "SetEnum", args);

        Assert.Equal(DayOfWeek.Friday, obj.LastEnum);
    }

    [Fact]
    public void InvokeMethod_ReturnsEnumAsString()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var result = _operations.InvokeMethod(id, "GetEnum", null);

        Assert.Equal("Wednesday", (result as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void InvokeMethod_AcceptsListOfIntegers()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"values\": [1, 2, 3, 4, 5]}") as JsonObject;

        _operations.InvokeMethod(id, "SetIntList", args);

        Assert.NotNull(obj.LastIntList);
        Assert.Equal([1, 2, 3, 4, 5], obj.LastIntList);
    }

    [Fact]
    public void InvokeMethod_ThrowsForMemoryType()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<NotSupportedException>(() =>
            _operations.InvokeMethod(id, "GetMemory", null));

        Assert.Contains("Memory<", ex.Message);
    }

    [Fact]
    public void InvokeMethod_ThrowsForAsyncEnumerable()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        var ex = Assert.Throws<NotSupportedException>(() =>
            _operations.InvokeMethod(id, "GetAsyncEnumerable", null));

        Assert.Contains("IAsyncEnumerable", ex.Message);
    }

    [Fact]
    public void InvokeMethod_ThrowsForInterfaceParameterWithoutReference()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        // Pass a plain object instead of a reference to an interface parameter
        var args = JsonNode.Parse("{\"service\": {\"Name\": \"test\"}}") as JsonObject;

        var ex = Assert.Throws<NotSupportedException>(() =>
            _operations.InvokeMethod(id, "TakeInterface", args));

        Assert.Contains("interface", ex.Message);
        Assert.Contains("$id", ex.Message);
    }

    [Fact]
    public void InvokeMethod_ThrowsForAbstractParameterWithoutReference()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var args = JsonNode.Parse("{\"resource\": {\"Name\": \"test\"}}") as JsonObject;

        var ex = Assert.Throws<NotSupportedException>(() =>
            _operations.InvokeMethod(id, "TakeAbstract", args));

        Assert.Contains("abstract", ex.Message);
        Assert.Contains("$id", ex.Message);
    }

    [Fact]
    public void InvokeMethod_AcceptsInterfaceWithReference()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);

        // Register an object that implements the interface
        var service = new ConcreteService { Name = "my-service" };
        var serviceId = _objectRegistry.Register(service);

        var args = JsonNode.Parse($"{{\"service\": {{\"$id\": \"{serviceId}\"}}}}") as JsonObject;

        _operations.InvokeMethod(id, "TakeInterface", args);

        Assert.Same(service, obj.LastService);
    }

    #endregion

    #region GetProperty Tests

    [Fact]
    public void GetProperty_ReturnsPropertyValue()
    {
        var obj = new TestObject { Name = "test" };
        var id = _objectRegistry.Register(obj);

        var result = _operations.GetProperty(id, "Name");

        Assert.Equal("test", (result as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void GetProperty_MarshallesComplexValue()
    {
        var nested = new TestObject { Name = "nested" };
        var obj = new TestObjectWithNested { Nested = nested };
        var id = _objectRegistry.Register(obj);

        var result = _operations.GetProperty(id, "Nested");

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("TestObject", jsonObj["$type"]!.GetValue<string>());
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
        var value = JsonNode.Parse("\"new value\"");

        _operations.SetProperty(id, "Name", value);

        Assert.Equal("new value", obj.Name);
    }

    [Fact]
    public void SetProperty_ThrowsForReadOnlyProperty()
    {
        var obj = new TestObject();
        var id = _objectRegistry.Register(obj);
        var value = JsonNode.Parse("42");

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
        var index = JsonNode.Parse("1")!;

        var result = _operations.GetIndexer(id, index);

        Assert.Equal("second", (result as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void GetIndexer_ReturnsDictionaryItem()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10, ["key2"] = 20 };
        var id = _objectRegistry.Register(dict);
        var key = JsonNode.Parse("\"key2\"")!;

        var result = _operations.GetIndexer(id, key);

        Assert.Equal(20, (result as JsonValue)?.GetValue<int>());
    }

    [Fact]
    public void GetIndexer_MarshallesComplexListItem()
    {
        var list = new List<TestObject> { new() { Name = "item1" } };
        var id = _objectRegistry.Register(list);
        var index = JsonNode.Parse("0")!;

        var result = _operations.GetIndexer(id, index);

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("TestObject", jsonObj["$type"]!.GetValue<string>());
    }

    [Fact]
    public void GetIndexer_ThrowsForOutOfRangeIndex()
    {
        var list = new List<string> { "only" };
        var id = _objectRegistry.Register(list);
        var index = JsonNode.Parse("5")!;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _operations.GetIndexer(id, index));
    }

    [Fact]
    public void GetIndexer_ReturnsNullForMissingDictionaryKey()
    {
        var dict = new Dictionary<string, int> { ["key1"] = 10 };
        var id = _objectRegistry.Register(dict);
        var key = JsonNode.Parse("\"nonexistent\"")!;

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
        var index = JsonNode.Parse("0")!;
        var value = JsonNode.Parse("\"updated\"");

        _operations.SetIndexer(id, index, value);

        Assert.Equal("updated", list[0]);
    }

    [Fact]
    public void SetIndexer_SetsDictionaryItem()
    {
        var dict = new Dictionary<string, object?> { ["key1"] = "old" };
        var id = _objectRegistry.Register(dict);
        var key = JsonNode.Parse("\"key1\"")!;
        var value = JsonNode.Parse("\"new\"");

        _operations.SetIndexer(id, key, value);

        Assert.Equal("new", dict["key1"]);
    }

    [Fact]
    public void SetIndexer_AddsDictionaryItem()
    {
        var dict = new Dictionary<string, object?>();
        var id = _objectRegistry.Register(dict);
        var key = JsonNode.Parse("\"newkey\"")!;
        var value = JsonNode.Parse("\"newvalue\"");

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

        var key = JsonNode.Parse("\"mykey\"")!;
        var value = JsonNode.Parse($"{{\"$id\": \"{refId}\"}}");

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

        Assert.Equal("test-value", (result as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void GetStaticProperty_ReturnsReadOnlyStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        var result = _operations.GetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue");

        Assert.Equal("readonly", (result as JsonValue)?.GetValue<string>());
    }

    [Fact]
    public void GetStaticProperty_MarshallesComplexValue()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;

        // Reset to known state
        StaticPropertyTestClass.ComplexStatic = new TestObject { Name = "complex-static" };

        var result = _operations.GetStaticProperty(assemblyName, typeName, "ComplexStatic");

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("TestObject", jsonObj["$type"]!.GetValue<string>());
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
        var value = JsonNode.Parse("\"new-value\"");

        _operations.SetStaticProperty(assemblyName, typeName, "StaticValue", value);

        Assert.Equal("new-value", StaticPropertyTestClass.StaticValue);
    }

    [Fact]
    public void SetStaticProperty_SetsIntStaticProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonNode.Parse("42");

        _operations.SetStaticProperty(assemblyName, typeName, "StaticInt", value);

        Assert.Equal(42, StaticPropertyTestClass.StaticInt);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForReadOnlyProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonNode.Parse("\"value\"");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty(assemblyName, typeName, "ReadOnlyStaticValue", value));

        Assert.Contains("read-only", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownType()
    {
        // Use the same assembly but a non-existent type
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var value = JsonNode.Parse("\"value\"");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty(assemblyName, "NonExistent.Type.That.DoesNotExist", "SomeProperty", value));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_ThrowsForUnknownProperty()
    {
        var assemblyName = typeof(StaticPropertyTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticPropertyTestClass).FullName!;
        var value = JsonNode.Parse("\"value\"");

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

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("SimpleTestClass", jsonObj["$type"]!.GetValue<string>());
        Assert.True(jsonObj.ContainsKey("$id"));
    }

    [Fact]
    public void CreateObject_CreatesInstanceWithConstructorArgs()
    {
        var assemblyName = typeof(TestClassWithArgs).Assembly.GetName().Name!;
        var typeName = typeof(TestClassWithArgs).FullName!;
        var args = JsonNode.Parse("{\"name\": \"test-name\", \"value\": 42}") as JsonObject;

        var result = _operations.CreateObject(assemblyName, typeName, args);

        var jsonObj = Assert.IsType<JsonObject>(result);
        var objectId = jsonObj["$id"]!.GetValue<string>();

        // Verify properties were set correctly
        var nameResult = _operations.GetProperty(objectId, "Name");
        Assert.Equal("test-name", (nameResult as JsonValue)?.GetValue<string>());

        var valueResult = _operations.GetProperty(objectId, "Value");
        Assert.Equal(42, (valueResult as JsonValue)?.GetValue<int>());
    }

    [Fact]
    public void CreateObject_UsesDefaultValuesForOptionalParameters()
    {
        var assemblyName = typeof(TestClassWithOptionalArgs).Assembly.GetName().Name!;
        var typeName = typeof(TestClassWithOptionalArgs).FullName!;
        var args = JsonNode.Parse("{\"name\": \"required-name\"}") as JsonObject;

        var result = _operations.CreateObject(assemblyName, typeName, args);

        var jsonObj = Assert.IsType<JsonObject>(result);
        var objectId = jsonObj["$id"]!.GetValue<string>();

        // Verify default value was used
        var valueResult = _operations.GetProperty(objectId, "Value");
        Assert.Equal(100, (valueResult as JsonValue)?.GetValue<int>());
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
        var args = JsonNode.Parse("{}") as JsonObject;

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
        var args = JsonNode.Parse("{\"a\": 10, \"b\": 5}") as JsonObject;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "Add", args);

        Assert.Equal(15, (result as JsonValue)?.GetValue<int>());
    }

    [Fact]
    public void InvokeStaticMethod_MarshallesComplexReturnValue()
    {
        var assemblyName = typeof(StaticTestClass).Assembly.GetName().Name!;
        var typeName = typeof(StaticTestClass).FullName!;
        var args = JsonNode.Parse("{\"name\": \"test-object\"}") as JsonObject;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "CreateInstance", args);

        var jsonObj = Assert.IsType<JsonObject>(result);
        Assert.Contains("StaticTestClass", jsonObj["$type"]!.GetValue<string>());
        Assert.True(jsonObj.ContainsKey("$id"));
    }

    [Fact]
    public void InvokeStaticMethod_ResolvesObjectReferences()
    {
        // Register an object and pass it as an argument
        var existingObj = new TestObject { Value = 42 };
        var objId = _objectRegistry.Register(existingObj);

        var assemblyName = typeof(ExtensionMethodTestClass).Assembly.GetName().Name!;
        var typeName = typeof(ExtensionMethodTestClass).FullName!;
        var args = JsonNode.Parse($"{{\"obj\": {{\"$id\": \"{objId}\"}}, \"amount\": 10}}") as JsonObject;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "IncrementValue", args);

        Assert.Equal(52, (result as JsonValue)?.GetValue<int>());
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
        var args = JsonNode.Parse($"{{\"builder\": {{\"$id\": \"{objId}\"}}, \"envName\": \"TEST_VAR\", \"envValue\": \"test-value\"}}") as JsonObject;

        var result = _operations.InvokeStaticMethod(assemblyName, typeName, "WithEnvironment", args);

        // The method returns the same object (marshalled as JsonObject)
        Assert.IsType<JsonObject>(result);
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
        var args1 = JsonNode.Parse("{\"value\": \"hello\"}") as JsonObject;
        var result1 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args1);
        Assert.Equal("[hello]", (result1 as JsonValue)?.GetValue<string>());

        // Two args with count - should use Format(string value, int count)
        var args2 = JsonNode.Parse("{\"value\": \"x\", \"count\": 2}") as JsonObject;
        var result2 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args2);
        Assert.Equal("[x][x]", (result2 as JsonValue)?.GetValue<string>());

        // Three string args - should use Format(string value, string prefix, string suffix)
        var args3 = JsonNode.Parse("{\"value\": \"test\", \"prefix\": \"<\", \"suffix\": \">\"}") as JsonObject;
        var result3 = _operations.InvokeStaticMethod(assemblyName, typeName, "Format", args3);
        Assert.Equal("<test>", (result3 as JsonValue)?.GetValue<string>());
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

        public int[] GetIntArray()
        {
            return [1, 2, 3, 4, 5];
        }

        public string[] GetStringArray()
        {
            return ["alpha", "beta", "gamma"];
        }

        public List<int> GetIntList()
        {
            return [10, 20, 30];
        }

        public Dictionary<string, int> GetStringIntDictionary()
        {
            return new() { ["one"] = 1, ["two"] = 2, ["three"] = 3 };
        }

        public Dictionary<string, string> GetStringStringDictionary()
        {
            return new() { ["key1"] = "value1", ["key2"] = "value2" };
        }

        public TestObject GetSelf()
        {
            return this;
        }

        public void MethodWithOptional(int value = 100)
        {
            Value = value;
        }

        // byte[] tests
        public byte[] GetByteArray()
        {
            return [0x48, 0x65, 0x6C, 0x6C, 0x6F]; // "Hello" in ASCII
        }

        public void SetByteArray(byte[] data)
        {
            LastByteArray = data;
        }

        public byte[]? LastByteArray { get; private set; }

        // Additional primitive types
        public float GetFloat() => 3.14f;
        public double GetDouble() => 2.71828;
        public decimal GetDecimal() => 123.456m;
        public short GetShort() => 12345;
        public ushort GetUShort() => 54321;
        public uint GetUInt() => 4000000000;
        public ulong GetULong() => 9000000000000000000UL;
        public char GetChar() => 'X';
        public Guid GetGuid() => new("12345678-1234-1234-1234-123456789012");
        public Uri GetUri() => new("https://example.com/path");
        public DateOnly GetDateOnly() => new(2024, 6, 15);
        public TimeOnly GetTimeOnly() => new(14, 30, 45);
        public DayOfWeek GetEnum() => DayOfWeek.Wednesday;

        public void SetFloat(float value) => LastFloat = value;
        public void SetDecimal(decimal value) => LastDecimal = value;
        public void SetGuid(Guid value) => LastGuid = value;
        public void SetUri(Uri value) => LastUri = value;
        public void SetDateOnly(DateOnly value) => LastDateOnly = value;
        public void SetTimeOnly(TimeOnly value) => LastTimeOnly = value;
        public void SetEnum(DayOfWeek value) => LastEnum = value;

        public float? LastFloat { get; private set; }
        public decimal? LastDecimal { get; private set; }
        public Guid? LastGuid { get; private set; }
        public Uri? LastUri { get; private set; }
        public DateOnly? LastDateOnly { get; private set; }
        public TimeOnly? LastTimeOnly { get; private set; }
        public DayOfWeek? LastEnum { get; private set; }

        // List<T> input tests
        public void SetIntList(List<int> values) => LastIntList = values;
        public List<int>? LastIntList { get; private set; }

        // Unsupported types - for testing rejection
        public Memory<byte> GetMemory() => new byte[10];
        public IAsyncEnumerable<int> GetAsyncEnumerable() => AsyncEnumerableStub();
        private static async IAsyncEnumerable<int> AsyncEnumerableStub()
        {
            await Task.Yield();
            yield return 1;
        }

        // Interface/abstract parameter tests
        public void TakeInterface(IService service) => LastService = service;
        public void TakeAbstract(AbstractResource resource) => LastResource = resource;
        public IService? LastService { get; private set; }
        public AbstractResource? LastResource { get; private set; }
    }

    // Test interface for POCO validation tests
    public interface IService
    {
        string Name { get; }
    }

    // Concrete implementation of IService
    public sealed class ConcreteService : IService
    {
        public string Name { get; set; } = "";
    }

    // Abstract class for POCO validation tests
    public abstract class AbstractResource
    {
        public abstract string Name { get; }
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
