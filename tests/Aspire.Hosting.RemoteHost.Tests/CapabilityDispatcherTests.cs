// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Ats;
using Aspire.Hosting.RemoteHost.Ats;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class CapabilityDispatcherTests
{
    [Fact]
    public void Register_AddsCapabilityThatCanBeInvoked()
    {
        var dispatcher = CreateDispatcher();
        CapabilityHandler handler = (args, handles) => Task.FromResult<JsonNode?>(JsonValue.Create("result"));

        dispatcher.Register("test/capability@1", handler);

        Assert.True(dispatcher.HasCapability("test/capability@1"));
    }

    [Fact]
    public void HasCapability_ReturnsFalseForUnregisteredCapability()
    {
        var dispatcher = CreateDispatcher();

        Assert.False(dispatcher.HasCapability("nonexistent/capability@1"));
    }

    [Fact]
    public void GetCapabilityIds_ReturnsAllRegisteredCapabilities()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/cap1@1", (_, _) => Task.FromResult<JsonNode?>(null));
        dispatcher.Register("test/cap2@1", (_, _) => Task.FromResult<JsonNode?>(null));

        var ids = dispatcher.GetCapabilityIds().ToList();

        Assert.Contains("test/cap1@1", ids);
        Assert.Contains("test/cap2@1", ids);
    }

    [Fact]
    public void Invoke_CallsRegisteredHandler()
    {
        var dispatcher = CreateDispatcher();
        var called = false;
        dispatcher.Register("test/capability@1", (args, handles) =>
        {
            called = true;
            return Task.FromResult<JsonNode?>(JsonValue.Create("success"));
        });

        dispatcher.Invoke("test/capability@1", null);

        Assert.True(called);
    }

    [Fact]
    public void Invoke_PassesArgumentsToHandler()
    {
        var dispatcher = CreateDispatcher();
        string? receivedName = null;
        dispatcher.Register("test/capability@1", (args, handles) =>
        {
            receivedName = args?["name"]?.GetValue<string>();
            return Task.FromResult<JsonNode?>(null);
        });

        dispatcher.Invoke("test/capability@1", new JsonObject { ["name"] = "test-value" });

        Assert.Equal("test-value", receivedName);
    }

    [Fact]
    public void Invoke_ReturnsHandlerResult()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) => Task.FromResult<JsonNode?>(JsonValue.Create(42)));

        var result = dispatcher.Invoke("test/capability@1", null);

        Assert.NotNull(result);
        Assert.Equal(42, result.GetValue<int>());
    }

    [Fact]
    public void Invoke_ThrowsCapabilityNotFoundForUnregisteredCapability()
    {
        var dispatcher = CreateDispatcher();

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("nonexistent/capability@1", null));

        Assert.Equal(AtsErrorCodes.CapabilityNotFound, ex.Error.Code);
    }

    [Fact]
    public void Invoke_WrapsHandlerExceptionsAsInternalError()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) =>
        {
            throw new InvalidOperationException("Handler failed");
#pragma warning disable CS0162 // Unreachable code detected - needed for return type inference
            return Task.FromResult<JsonNode?>(null);
#pragma warning restore CS0162
        });

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/capability@1", null));

        Assert.Equal(AtsErrorCodes.InternalError, ex.Error.Code);
        Assert.Contains("Handler failed", ex.Message);
    }

    [Fact]
    public void Invoke_PropagatesCapabilityExceptionsDirectly()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) =>
        {
            throw CapabilityException.InvalidArgument("test/capability@1", "param", "Bad value");
#pragma warning disable CS0162 // Unreachable code detected - needed for return type inference
            return Task.FromResult<JsonNode?>(null);
#pragma warning restore CS0162
        });

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/capability@1", null));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
    }

    [Fact]
    public void Invoke_ConvertsInvalidCastToTypeMismatch()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) =>
        {
            throw new InvalidCastException("Cannot cast to expected type");
#pragma warning disable CS0162 // Unreachable code detected - needed for return type inference
            return Task.FromResult<JsonNode?>(null);
#pragma warning restore CS0162
        });

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/capability@1", null));

        Assert.Equal(AtsErrorCodes.TypeMismatch, ex.Error.Code);
    }

    [Fact]
    public void Constructor_ScansAssemblyForAspireExportAttributes()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);

        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/testMethod"));
    }

    [Fact]
    public void Invoke_CanCallScannedCapability()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "hello" };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/testMethod", args);

        Assert.NotNull(result);
        Assert.Equal("HELLO", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_HandlesOptionalParameters()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["required"] = "test" };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/withOptional", args);

        Assert.NotNull(result);
        Assert.Equal("test:default", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_UsesProvidedOptionalParameter()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["required"] = "test", ["optional"] = "custom" };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/withOptional", args);

        Assert.NotNull(result);
        Assert.Equal("test:custom", result.GetValue<string>());
    }

    // Context type tests
    [Fact]
    public void Constructor_RegistersContextTypeProperties()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        // Properties should be registered with getter capabilities using derived type IDs
        // Type ID = {AssemblyName}/{FullTypeName}
        // Capability ID = {Package}/{TypeName}.{propertyName} (camelCase, no "get" prefix)
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.name"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.count"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.isEnabled"));
    }

    [Fact]
    public void Constructor_SkipsNonAtsCompatibleProperties()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        // IDisposable is not ATS-compatible, so this property should be skipped
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.nonAtsProperty"));
    }

    [Fact]
    public void Invoke_ContextTypePropertyReturnsValue()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        // Create and register a context object
        // Type ID = {AssemblyName}/{FullTypeName}
        var context = new TestContextType { Name = "test-name", Count = 100 };
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType");
        var args = new JsonObject { ["context"] = new JsonObject { ["$handle"] = handleId } };

        var nameResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.name", args);
        var countResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.count", args);

        Assert.NotNull(nameResult);
        Assert.Equal("test-name", nameResult.GetValue<string>());
        Assert.NotNull(countResult);
        Assert.Equal(100, countResult.GetValue<int>());
    }

    [Fact]
    public void Invoke_ContextTypePropertyThrowsWhenContextMissing()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.name", null));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
        Assert.Contains("context", ex.Message);
    }

    [Fact]
    public void Invoke_ContextTypePropertyThrowsWhenContextNotHandle()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);
        var args = new JsonObject { ["context"] = "not-a-handle" };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.name", args));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
    }

    [Fact]
    public void Invoke_ContextTypePropertyThrowsWhenHandleNotFound()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);
        var args = new JsonObject { ["context"] = new JsonObject { ["$handle"] = "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType:999" } };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.name", args));

        Assert.Equal(AtsErrorCodes.HandleNotFound, ex.Error.Code);
    }

    [Fact]
    public void Constructor_RegistersVersionedContextTypeProperties()
    {
        var dispatcher = CreateDispatcher(typeof(VersionedContextType).Assembly);

        // Versioned context type properties should also be registered with derived type ID
        // Capability ID = {Package}/{TypeName}.{propertyName} (camelCase, no "get" prefix)
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/VersionedContextType.value"));
    }

    // Property setter tests
    [Fact]
    public void Constructor_RegistersPropertySetters()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        // Property setters should be registered with "set" prefix and PascalCase property name
        // Capability ID = {Package}/{TypeName}.set{PropertyName}
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.setName"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.setCount"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestContextType.setIsEnabled"));
    }

    [Fact]
    public void Invoke_PropertySetterUpdatesValue()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        // Create and register a context object
        var context = new TestContextType { Name = "original-name" };
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId },
            ["value"] = "new-name"
        };

        // Invoke the setter
        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.setName", args);

        // Verify the value was updated
        Assert.Equal("new-name", context.Name);
    }

    [Fact]
    public void Invoke_PropertySetterUpdatesIntValue()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        var context = new TestContextType { Count = 10 };
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId },
            ["value"] = 99
        };

        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.setCount", args);

        Assert.Equal(99, context.Count);
    }

    [Fact]
    public void Invoke_PropertySetterUpdatesBoolValue()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        var context = new TestContextType { IsEnabled = true };
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId },
            ["value"] = false
        };

        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.setIsEnabled", args);

        Assert.False(context.IsEnabled);
    }

    [Fact]
    public void Invoke_PropertySetterThrowsWhenValueMissing()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        var context = new TestContextType();
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestContextType");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId }
            // Missing "value" parameter
        };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestContextType.setName", args));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
        Assert.Contains("value", ex.Message);
    }

    // Instance method tests
    [Fact]
    public void Constructor_RegistersInstanceMethods()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeWithMethods).Assembly);

        // Instance methods should be registered with capability ID format: {Package}/{TypeName}.{methodName}
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.doSomething"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.calculateSum"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.processAsync"));
    }

    [Fact]
    public void Invoke_InstanceMethodCallsMethodOnContext()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeWithMethods).Assembly]);

        var context = new TestTypeWithMethods();
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestTypeWithMethods");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId }
        };

        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.doSomething", args);

        Assert.True(context.WasCalled);
    }

    [Fact]
    public void Invoke_InstanceMethodWithParameters()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeWithMethods).Assembly]);

        var context = new TestTypeWithMethods();
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestTypeWithMethods");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId },
            ["a"] = 5,
            ["b"] = 7
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.calculateSum", args);

        Assert.NotNull(result);
        Assert.Equal(12, result.GetValue<int>());
    }

    [Fact]
    public void Invoke_AsyncInstanceMethod()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeWithMethods).Assembly]);

        var context = new TestTypeWithMethods();
        var handleId = handles.Register(context, "Aspire.Hosting.RemoteHost.Tests/Aspire.Hosting.RemoteHost.Tests.TestTypeWithMethods");
        var args = new JsonObject
        {
            ["context"] = new JsonObject { ["$handle"] = handleId },
            ["input"] = "test"
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.processAsync", args);

        Assert.NotNull(result);
        Assert.Equal("TEST", result.GetValue<string>());
    }

    [Fact]
    public void Constructor_SkipsNonPublicMethods()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeWithMethods).Assembly);

        // Private methods should not be exposed
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.privateMethod"));
    }

    [Fact]
    public void Constructor_SkipsSpecialMethods()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeWithMethods).Assembly);

        // Property getters/setters via ExposeMethods should not create duplicate capabilities
        // (they are handled by ExposeProperties separately)
        // Object methods like ToString, GetHashCode should be skipped
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.toString"));
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.getHashCode"));
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.equals"));
        Assert.False(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/TestTypeWithMethods.getType"));
    }

    // Callback parameter tests
    [Fact]
    public void Invoke_MethodWithCallbackParameter()
    {
        var handles = new HandleRegistry();
        var invoker = new TestCallbackInvoker();
        using var callbackFactory = new AtsCallbackProxyFactory(invoker, handles);
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestCapabilitiesWithCallback).Assembly], callbackFactory);

        var args = new JsonObject
        {
            ["value"] = "test-input",
            ["callback"] = "my-callback-id"
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/withCallback", args);

        // The method should have been called with the callback proxy
        // The callback proxy invokes our TestCallbackInvoker
        Assert.NotNull(result);
        Assert.Equal("PROCESSED: test-input", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_MethodWithCallbackParameter_InvokesCallback()
    {
        var handles = new HandleRegistry();
        var invoker = new TestCallbackInvoker();
        using var callbackFactory = new AtsCallbackProxyFactory(invoker, handles);
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestCapabilitiesWithCallback).Assembly], callbackFactory);

        var args = new JsonObject
        {
            ["callback"] = "progress-callback"
        };

        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/invokeCallback", args);

        // Verify the callback was invoked
        Assert.Single(invoker.Invocations);
        Assert.Equal("progress-callback", invoker.Invocations[0].CallbackId);
    }

    [Fact]
    public void Invoke_MethodWithCallbackParameter_PassesArgsToCallback()
    {
        var handles = new HandleRegistry();
        var invoker = new TestCallbackInvoker();
        using var callbackFactory = new AtsCallbackProxyFactory(invoker, handles);
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestCapabilitiesWithCallback).Assembly], callbackFactory);

        var args = new JsonObject
        {
            ["callback"] = "typed-callback"
        };

        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/invokeTypedCallback", args);

        Assert.Single(invoker.Invocations);
        var callbackArgs = invoker.Invocations[0].Args as JsonObject;
        Assert.NotNull(callbackArgs);
        // For Func<string, Task>, the parameter name is "arg" (generated by compiler)
        Assert.Equal("hello from C#", callbackArgs["arg"]?.GetValue<string>());
    }

    [Fact]
    public void Invoke_MethodWithAsyncCallback()
    {
        var handles = new HandleRegistry();
        var invoker = new TestCallbackInvoker { ResultToReturn = JsonValue.Create(42) };
        using var callbackFactory = new AtsCallbackProxyFactory(invoker, handles);
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestCapabilitiesWithCallback).Assembly], callbackFactory);

        var args = new JsonObject
        {
            ["callback"] = "async-callback"
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/withAsyncCallback", args);

        Assert.NotNull(result);
        Assert.Equal(42, result.GetValue<int>());
    }

    [Fact]
    public void Invoke_MethodWithoutCallbackFactory_ThrowsForCallbackParameter()
    {
        var handles = new HandleRegistry();
        // No callback factory provided
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestCapabilitiesWithCallback).Assembly], callbackProxyFactory: null);

        var args = new JsonObject
        {
            ["value"] = "test",
            ["callback"] = "some-callback"
        };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/withCallback", args));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
        Assert.Contains("callback", ex.Message.ToLower());
    }

    // Async capability handler tests
    [Fact]
    public void Constructor_ScansAsyncMethods()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);

        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/asyncVoid"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/asyncWithResult"));
        Assert.True(dispatcher.HasCapability("Aspire.Hosting.RemoteHost.Tests/asyncThrows"));
    }

    [Fact]
    public void Invoke_HandlesAsyncVoidMethod()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "test" };

        // Should not throw - async void (Task) methods complete successfully
        // Note: may return the awaited Task's internal result or null
        dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/asyncVoid", args);
    }

    [Fact]
    public void Invoke_HandlesAsyncMethodWithResult()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "hello" };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/asyncWithResult", args);

        Assert.NotNull(result);
        Assert.Equal("HELLO", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_HandlesAsyncMethodThatThrows()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "test" };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/asyncThrows", args));

        Assert.Equal(AtsErrorCodes.InternalError, ex.Error.Code);
        Assert.Contains("Async error", ex.Message);
    }

    // Array Marshalling tests
    [Fact]
    public void Invoke_AcceptsArrayParameter()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeCategoryCapabilities).Assembly);
        var args = new JsonObject
        {
            ["values"] = new JsonArray { 1, 2, 3, 4, 5 }
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/sumArray", args);

        Assert.NotNull(result);
        Assert.Equal(15, result.GetValue<int>());
    }

    [Fact]
    public void Invoke_ReturnsArrayResult()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeCategoryCapabilities).Assembly);
        var args = new JsonObject { ["count"] = 3 };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnArray", args);

        Assert.NotNull(result);
        var resultArray = result as JsonArray;
        Assert.NotNull(resultArray);
        Assert.Equal(3, resultArray.Count);
        Assert.Equal("item0", resultArray[0]?.GetValue<string>());
        Assert.Equal("item1", resultArray[1]?.GetValue<string>());
        Assert.Equal("item2", resultArray[2]?.GetValue<string>());
    }

    [Fact]
    public void Invoke_AcceptsReadOnlyListParameter()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeCategoryCapabilities).Assembly);
        var args = new JsonObject
        {
            ["values"] = new JsonArray { 10, 20, 30 }
        };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/acceptReadOnlyList", args);

        Assert.NotNull(result);
        Assert.Equal(60, result.GetValue<int>());
    }

    // Union type tests
    [Fact]
    public void Invoke_AcceptsUnionWithString()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeCategoryCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "hello" };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/acceptUnion", args);

        Assert.NotNull(result);
        Assert.Equal("hello", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_AcceptsUnionWithInt()
    {
        var dispatcher = CreateDispatcher(typeof(TestTypeCategoryCapabilities).Assembly);
        var args = new JsonObject { ["value"] = 42 };

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/acceptUnion", args);

        Assert.NotNull(result);
        Assert.Equal("42", result.GetValue<string>());
    }

    // List operations tests
    [Fact]
    public void Invoke_ReturnsMutableListAsHandle()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly]);

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableList", null);

        // Mutable lists should be returned as handles
        Assert.NotNull(result);
        var resultObj = result as JsonObject;
        Assert.NotNull(resultObj);
        Assert.True(resultObj.ContainsKey("$handle"));
    }

    [Fact]
    public void Invoke_ListGet_ReturnsItemAtIndex()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a list and get its handle
        var listResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableList", null);
        var listHandle = (listResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(listHandle);

        // Get item at index 1
        var args = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle },
            ["index"] = 1
        };

        var result = dispatcher.Invoke("Aspire.Hosting/List.get", args);

        Assert.NotNull(result);
        Assert.Equal("second", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_ListRemoveAt_RemovesItem()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a list and get its handle
        var listResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableList", null);
        var listHandle = (listResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(listHandle);

        // Remove item at index 1 ("second")
        var removeArgs = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle },
            ["index"] = 1
        };
        var removeResult = dispatcher.Invoke("Aspire.Hosting/List.removeAt", removeArgs);

        Assert.NotNull(removeResult);
        Assert.True(removeResult.GetValue<bool>());

        // Verify length is now 2
        var lengthArgs = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle }
        };
        var lengthResult = dispatcher.Invoke("Aspire.Hosting/List.length", lengthArgs);

        Assert.NotNull(lengthResult);
        Assert.Equal(2, lengthResult.GetValue<int>());
    }

    [Fact]
    public void Invoke_ListLength_ReturnsCount()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a list and get its handle
        var listResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableList", null);
        var listHandle = (listResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(listHandle);

        var args = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle }
        };

        var result = dispatcher.Invoke("Aspire.Hosting/List.length", args);

        Assert.NotNull(result);
        Assert.Equal(3, result.GetValue<int>());
    }

    [Fact]
    public void Invoke_ListClear_RemovesAllItems()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a list and get its handle
        var listResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableList", null);
        var listHandle = (listResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(listHandle);

        // Clear the list
        var clearArgs = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle }
        };
        dispatcher.Invoke("Aspire.Hosting/List.clear", clearArgs);

        // Verify length is 0
        var lengthArgs = new JsonObject
        {
            ["list"] = new JsonObject { ["$handle"] = listHandle }
        };
        var lengthResult = dispatcher.Invoke("Aspire.Hosting/List.length", lengthArgs);

        Assert.NotNull(lengthResult);
        Assert.Equal(0, lengthResult.GetValue<int>());
    }

    // Dict operations tests
    [Fact]
    public void Invoke_ReturnsMutableDictAsHandle()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly]);

        var result = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);

        // Mutable dicts should be returned as handles
        Assert.NotNull(result);
        var resultObj = result as JsonObject;
        Assert.NotNull(resultObj);
        Assert.True(resultObj.ContainsKey("$handle"));
    }

    [Fact]
    public void Invoke_DictGet_ReturnsValueForKey()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a dict and get its handle
        var dictResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);
        var dictHandle = (dictResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(dictHandle);

        // Get value for "key1"
        var args = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle },
            ["key"] = "key1"
        };

        var result = dispatcher.Invoke("Aspire.Hosting/Dict.get", args);

        Assert.NotNull(result);
        Assert.Equal("value1", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_DictRemove_RemovesKey()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a dict and get its handle
        var dictResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);
        var dictHandle = (dictResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(dictHandle);

        // Remove "key1"
        var removeArgs = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle },
            ["key"] = "key1"
        };
        var removeResult = dispatcher.Invoke("Aspire.Hosting/Dict.remove", removeArgs);

        Assert.NotNull(removeResult);
        Assert.True(removeResult.GetValue<bool>());

        // Verify count is now 1
        var countArgs = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle }
        };
        var countResult = dispatcher.Invoke("Aspire.Hosting/Dict.count", countArgs);

        Assert.NotNull(countResult);
        Assert.Equal(1, countResult.GetValue<int>());
    }

    [Fact]
    public void Invoke_DictHas_ReturnsTrueForExistingKey()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a dict and get its handle
        var dictResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);
        var dictHandle = (dictResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(dictHandle);

        // Check for existing key
        var hasArgs = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle },
            ["key"] = "key1"
        };
        var hasResult = dispatcher.Invoke("Aspire.Hosting/Dict.has", hasArgs);

        Assert.NotNull(hasResult);
        Assert.True(hasResult.GetValue<bool>());

        // Check for non-existing key
        var hasArgs2 = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle },
            ["key"] = "nonexistent"
        };
        var hasResult2 = dispatcher.Invoke("Aspire.Hosting/Dict.has", hasArgs2);

        Assert.NotNull(hasResult2);
        Assert.False(hasResult2.GetValue<bool>());
    }

    [Fact]
    public void Invoke_DictKeys_ReturnsAllKeys()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a dict and get its handle
        var dictResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);
        var dictHandle = (dictResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(dictHandle);

        var args = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle }
        };

        var result = dispatcher.Invoke("Aspire.Hosting/Dict.keys", args);

        Assert.NotNull(result);
        var keysArray = result as JsonArray;
        Assert.NotNull(keysArray);
        Assert.Equal(2, keysArray.Count);
        Assert.Contains("key1", keysArray.Select(k => k?.GetValue<string>()));
        Assert.Contains("key2", keysArray.Select(k => k?.GetValue<string>()));
    }

    [Fact]
    public void Invoke_DictCount_ReturnsEntryCount()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestTypeCategoryCapabilities).Assembly, typeof(AspireExportAttribute).Assembly]);

        // Create a dict and get its handle
        var dictResult = dispatcher.Invoke("Aspire.Hosting.RemoteHost.Tests/returnMutableDict", null);
        var dictHandle = (dictResult as JsonObject)?["$handle"]?.GetValue<string>();
        Assert.NotNull(dictHandle);

        var args = new JsonObject
        {
            ["dict"] = new JsonObject { ["$handle"] = dictHandle }
        };

        var result = dispatcher.Invoke("Aspire.Hosting/Dict.count", args);

        Assert.NotNull(result);
        Assert.Equal(2, result.GetValue<int>());
    }

    private static CapabilityDispatcher CreateDispatcher(params System.Reflection.Assembly[] assemblies)
    {
        var handles = new HandleRegistry();
        return new CapabilityDispatcher(handles, assemblies);
    }
}

/// <summary>
/// Test capabilities for scanning.
/// </summary>
internal static class TestCapabilities
{
    [AspireExport("testMethod", Description = "Test method")]
    public static string TestMethod(string value)
    {
        return value.ToUpperInvariant();
    }

    [AspireExport("withOptional", Description = "Method with optional parameter")]
    public static string WithOptional(string required, string optional = "default")
    {
        return $"{required}:{optional}";
    }

    [AspireExport("asyncVoid", Description = "Async method returning Task")]
    public static async Task AsyncVoidMethod(string value)
    {
        await Task.Delay(1);
        _ = value; // Use the parameter to avoid warning
    }

    [AspireExport("asyncWithResult", Description = "Async method returning Task<T>")]
    public static async Task<string> AsyncWithResult(string value)
    {
        await Task.Delay(1);
        return value.ToUpperInvariant();
    }

    [AspireExport("asyncThrows", Description = "Async method that throws")]
    public static async Task<string> AsyncThrows(string value)
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Async error: " + value);
    }
}

/// <summary>
/// Test context type for context type tests.
/// </summary>
[AspireExport(ExposeProperties = true)]
internal sealed class TestContextType
{
    public string Name { get; set; } = "default-name";
    public int Count { get; set; } = 42;
    public bool IsEnabled { get; set; } = true;

    // This property should be skipped - IDisposable is not ATS-compatible
    public IDisposable? NonAtsProperty { get; set; }
}

/// <summary>
/// Test context type to verify context properties work.
/// </summary>
[AspireExport(ExposeProperties = true)]
internal sealed class VersionedContextType
{
    public string Value { get; set; } = "v2";
}

/// <summary>
/// Test type for instance method tests.
/// </summary>
[AspireExport(ExposeMethods = true)]
internal sealed class TestTypeWithMethods
{
    public bool WasCalled { get; private set; }

    public void DoSomething()
    {
        WasCalled = true;
    }

#pragma warning disable CA1822 // Mark members as static - testing instance methods
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }

    public async Task<string> ProcessAsync(string input)
    {
        await Task.Delay(1);
        return input.ToUpperInvariant();
    }

    // This should NOT be exposed - private method
#pragma warning disable IDE0051 // Remove unused private member - testing that private methods are not exposed
    private void PrivateMethod()
    {
    }
#pragma warning restore IDE0051
#pragma warning restore CA1822
}

/// <summary>
/// Test capabilities with callback parameters.
/// </summary>
internal static class TestCapabilitiesWithCallback
{
    /// <summary>
    /// A method that accepts a callback but doesn't invoke it.
    /// </summary>
    [AspireExport("withCallback", Description = "Method with callback parameter")]
    public static string WithCallback(string value, Action callback)
    {
        // The callback is provided but we don't invoke it in this test
        _ = callback;
        return $"PROCESSED: {value}";
    }

    /// <summary>
    /// A method that invokes the callback.
    /// </summary>
    [AspireExport("invokeCallback", Description = "Method that invokes callback")]
    public static void InvokeCallback(Func<Task> callback)
    {
        callback().GetAwaiter().GetResult();
    }

    /// <summary>
    /// A method that invokes a typed callback with arguments.
    /// </summary>
    [AspireExport("invokeTypedCallback", Description = "Method that invokes typed callback")]
    public static void InvokeTypedCallback(Func<string, Task> callback)
    {
        callback("hello from C#").GetAwaiter().GetResult();
    }

    /// <summary>
    /// A method with an async callback that returns a value.
    /// </summary>
    [AspireExport("withAsyncCallback", Description = "Method with async callback returning value")]
    public static int WithAsyncCallback(Func<Task<int>> callback)
    {
        return callback().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Test capabilities for Array, Union type, List, and Dict marshalling.
/// </summary>
internal static class TestTypeCategoryCapabilities
{
    [AspireExport("sumArray", Description = "Sums an integer array")]
    public static int SumArray(int[] values)
    {
        return values.Sum();
    }

    [AspireExport("returnArray", Description = "Returns a string array")]
    public static string[] ReturnArray(int count)
    {
        return Enumerable.Range(0, count).Select(i => $"item{i}").ToArray();
    }

    [AspireExport("acceptReadOnlyList", Description = "Accepts a readonly list")]
    public static int SumReadOnlyList(IReadOnlyList<int> values)
    {
        return values.Sum();
    }

    [AspireExport("acceptUnion", Description = "Accepts a union of string or int")]
    public static string AcceptUnion([AspireUnion(typeof(string), typeof(int))] object value)
    {
        return value.ToString()!;
    }

    [AspireExport("returnMutableList", Description = "Returns a mutable List<string>")]
    public static List<object> ReturnMutableList()
    {
        return ["first", "second", "third"];
    }

    [AspireExport("returnMutableDict", Description = "Returns a mutable Dictionary<string, object>")]
    public static Dictionary<string, object> ReturnMutableDict()
    {
        return new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };
    }
}
