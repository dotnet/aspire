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
        CapabilityHandler handler = (args, handles) => JsonValue.Create("result");

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
        dispatcher.Register("test/cap1@1", (_, _) => null);
        dispatcher.Register("test/cap2@1", (_, _) => null);

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
            return JsonValue.Create("success");
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
            return null;
        });

        dispatcher.Invoke("test/capability@1", new JsonObject { ["name"] = "test-value" });

        Assert.Equal("test-value", receivedName);
    }

    [Fact]
    public void Invoke_ReturnsHandlerResult()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) => JsonValue.Create(42));

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
            throw new InvalidOperationException("Handler failed"));

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
            throw CapabilityException.InvalidArgument("test/capability@1", "param", "Bad value"));

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/capability@1", null));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
    }

    [Fact]
    public void Invoke_ConvertsInvalidCastToTypeMismatch()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Register("test/capability@1", (_, _) =>
            throw new InvalidCastException("Cannot cast to expected type"));

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/capability@1", null));

        Assert.Equal(AtsErrorCodes.TypeMismatch, ex.Error.Code);
    }

    [Fact]
    public void Constructor_ScansAssemblyForAspireExportAttributes()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);

        Assert.True(dispatcher.HasCapability("test/testMethod@1"));
    }

    [Fact]
    public void Invoke_CanCallScannedCapability()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "hello" };

        var result = dispatcher.Invoke("test/testMethod@1", args);

        Assert.NotNull(result);
        Assert.Equal("HELLO", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_HandlesOptionalParameters()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["required"] = "test" };

        var result = dispatcher.Invoke("test/withOptional@1", args);

        Assert.NotNull(result);
        Assert.Equal("test:default", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_UsesProvidedOptionalParameter()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["required"] = "test", ["optional"] = "custom" };

        var result = dispatcher.Invoke("test/withOptional@1", args);

        Assert.NotNull(result);
        Assert.Equal("test:custom", result.GetValue<string>());
    }

    // Context type tests
    [Fact]
    public void Constructor_RegistersContextTypeProperties()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        // Properties should be registered with camelCase names
        Assert.True(dispatcher.HasCapability("test/TestContext.name@1"));
        Assert.True(dispatcher.HasCapability("test/TestContext.count@1"));
        Assert.True(dispatcher.HasCapability("test/TestContext.isEnabled@1"));
    }

    [Fact]
    public void Constructor_SkipsNonAtsCompatibleProperties()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);

        // IDisposable is not ATS-compatible, so this property should be skipped
        Assert.False(dispatcher.HasCapability("test/TestContext.nonAtsProperty@1"));
    }

    [Fact]
    public void Invoke_ContextTypePropertyReturnsValue()
    {
        var handles = new HandleRegistry();
        var dispatcher = new CapabilityDispatcher(handles, [typeof(TestContextType).Assembly]);

        // Create and register a context object
        var context = new TestContextType { Name = "test-name", Count = 100 };
        var handleId = handles.Register(context, "test/TestContext");
        var args = new JsonObject { ["context"] = new JsonObject { ["$handle"] = handleId } };

        var nameResult = dispatcher.Invoke("test/TestContext.name@1", args);
        var countResult = dispatcher.Invoke("test/TestContext.count@1", args);

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
            dispatcher.Invoke("test/TestContext.name@1", null));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
        Assert.Contains("context", ex.Message);
    }

    [Fact]
    public void Invoke_ContextTypePropertyThrowsWhenContextNotHandle()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);
        var args = new JsonObject { ["context"] = "not-a-handle" };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/TestContext.name@1", args));

        Assert.Equal(AtsErrorCodes.InvalidArgument, ex.Error.Code);
    }

    [Fact]
    public void Invoke_ContextTypePropertyThrowsWhenHandleNotFound()
    {
        var dispatcher = CreateDispatcher(typeof(TestContextType).Assembly);
        var args = new JsonObject { ["context"] = new JsonObject { ["$handle"] = "test/TestContext:999" } };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/TestContext.name@1", args));

        Assert.Equal(AtsErrorCodes.HandleNotFound, ex.Error.Code);
    }

    [Fact]
    public void Constructor_UsesVersionFromAttribute()
    {
        var dispatcher = CreateDispatcher(typeof(VersionedContextType).Assembly);

        // Version 2 should be used in the capability ID
        Assert.True(dispatcher.HasCapability("test/VersionedContext.value@2"));
        Assert.False(dispatcher.HasCapability("test/VersionedContext.value@1"));
    }

    // Async capability handler tests
    [Fact]
    public void Constructor_ScansAsyncMethods()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);

        Assert.True(dispatcher.HasCapability("test/asyncVoid@1"));
        Assert.True(dispatcher.HasCapability("test/asyncWithResult@1"));
        Assert.True(dispatcher.HasCapability("test/asyncThrows@1"));
    }

    [Fact]
    public void Invoke_HandlesAsyncVoidMethod()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "test" };

        // Should not throw - async void (Task) methods complete successfully
        // Note: may return the awaited Task's internal result or null
        dispatcher.Invoke("test/asyncVoid@1", args);
    }

    [Fact]
    public void Invoke_HandlesAsyncMethodWithResult()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "hello" };

        var result = dispatcher.Invoke("test/asyncWithResult@1", args);

        Assert.NotNull(result);
        Assert.Equal("HELLO", result.GetValue<string>());
    }

    [Fact]
    public void Invoke_HandlesAsyncMethodThatThrows()
    {
        var dispatcher = CreateDispatcher(typeof(TestCapabilities).Assembly);
        var args = new JsonObject { ["value"] = "test" };

        var ex = Assert.Throws<CapabilityException>(() =>
            dispatcher.Invoke("test/asyncThrows@1", args));

        Assert.Equal(AtsErrorCodes.InternalError, ex.Error.Code);
        Assert.Contains("Async error", ex.Message);
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
    [AspireExport("test/testMethod@1", Description = "Test method")]
    public static string TestMethod(string value)
    {
        return value.ToUpperInvariant();
    }

    [AspireExport("test/withOptional@1", Description = "Method with optional parameter")]
    public static string WithOptional(string required, string optional = "default")
    {
        return $"{required}:{optional}";
    }

    [AspireExport("test/asyncVoid@1", Description = "Async method returning Task")]
    public static async Task AsyncVoidMethod(string value)
    {
        await Task.Delay(1);
        _ = value; // Use the parameter to avoid warning
    }

    [AspireExport("test/asyncWithResult@1", Description = "Async method returning Task<T>")]
    public static async Task<string> AsyncWithResult(string value)
    {
        await Task.Delay(1);
        return value.ToUpperInvariant();
    }

    [AspireExport("test/asyncThrows@1", Description = "Async method that throws")]
    public static async Task<string> AsyncThrows(string value)
    {
        await Task.Delay(1);
        throw new InvalidOperationException("Async error: " + value);
    }
}

/// <summary>
/// Test context type for context type tests.
/// </summary>
[AspireContextType("test/TestContext")]
internal sealed class TestContextType
{
    public string Name { get; set; } = "default-name";
    public int Count { get; set; } = 42;
    public bool IsEnabled { get; set; } = true;

    // This property should be skipped - IDisposable is not ATS-compatible
    public IDisposable? NonAtsProperty { get; set; }
}

/// <summary>
/// Test context type with custom version.
/// </summary>
[AspireContextType("test/VersionedContext", Version = 2)]
internal sealed class VersionedContextType
{
    public string Value { get; set; } = "v2";
}
