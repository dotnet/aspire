// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class SecurityPolicyTests
{
    [Theory]
    [InlineData("Aspire.Hosting")]
    [InlineData("Aspire.Hosting.Redis")]
    [InlineData("Aspire.Hosting.PostgreSQL")]
    [InlineData("Microsoft.Extensions.Hosting")]
    [InlineData("Microsoft.Extensions.Configuration")]
    [InlineData("Microsoft.Extensions.DependencyInjection")]
    [InlineData("Microsoft.Extensions.ServiceDiscovery")]
    public void IsAssemblyAllowed_AllowedAssemblies_ReturnsTrue(string assemblyName)
    {
        var policy = SecurityPolicy.Default;

        Assert.True(policy.IsAssemblyAllowed(assemblyName));
    }

    [Theory]
    [InlineData("System.IO")]
    [InlineData("System.Diagnostics")]
    [InlineData("System.Reflection")]
    [InlineData("System.Net.Http")]
    [InlineData("mscorlib")]
    [InlineData("System.Private.CoreLib")]
    [InlineData("Newtonsoft.Json")]
    [InlineData("SomeMaliciousPackage")]
    public void IsAssemblyAllowed_BlockedAssemblies_ReturnsFalse(string assemblyName)
    {
        var policy = SecurityPolicy.Default;

        Assert.False(policy.IsAssemblyAllowed(assemblyName));
    }

    [Fact]
    public void ValidateAssemblyAccess_BlockedAssembly_ThrowsUnauthorizedAccessException()
    {
        var policy = SecurityPolicy.Default;

        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            policy.ValidateAssemblyAccess("System.IO"));

        Assert.Equal("Access denied.", ex.Message);
    }

    [Fact]
    public void ValidateAssemblyAccess_AllowedAssembly_DoesNotThrow()
    {
        var policy = SecurityPolicy.Default;

        // Should not throw
        policy.ValidateAssemblyAccess("Aspire.Hosting.Redis");
    }
}

public class AuthenticationTests
{
    [Fact]
    public void Authenticate_WithCorrectToken_Succeeds()
    {
        var service = new RemoteAppHostService("secret-token-123");

        var result = service.Authenticate("secret-token-123");

        Assert.True(result);
    }

    [Fact]
    public void Authenticate_WithIncorrectToken_ThrowsUnauthorizedAccessException()
    {
        var service = new RemoteAppHostService("secret-token-123");

        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            service.Authenticate("wrong-token"));

        Assert.Equal("Access denied.", ex.Message);
    }

    [Fact]
    public void Authenticate_WithNoTokenRequired_AlwaysSucceeds()
    {
        var service = new RemoteAppHostService(null);

        var result = service.Authenticate("any-token");

        Assert.True(result);
    }

    [Fact]
    public void RpcMethods_BeforeAuthentication_ThrowUnauthorizedAccessException()
    {
        var service = new RemoteAppHostService("secret-token");

        // Trying to invoke a method without authenticating should fail
        var ex = Assert.Throws<UnauthorizedAccessException>(() =>
            service.InvokeMethod("obj_1", "SomeMethod", null));

        Assert.Equal("Access denied.", ex.Message);
    }

    [Fact]
    public void RpcMethods_AfterAuthentication_Succeed()
    {
        var service = new RemoteAppHostService("secret-token");

        // Authenticate first
        service.Authenticate("secret-token");

        // Now methods should work (will fail for other reasons like object not found, but not auth)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            service.InvokeMethod("obj_1", "SomeMethod", null));

        // The error should be about object not found, not authentication
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Ping_DoesNotRequireAuthentication()
    {
        var service = new RemoteAppHostService("secret-token");

        // Ping should work without authentication
        var result = service.Ping();

        Assert.Equal("pong", result);
    }
}

public class RpcOperationsSecurityTests : IAsyncLifetime
{
    private readonly ObjectRegistry _objectRegistry;
    private readonly TestCallbackInvoker _callbackInvoker;
    private readonly RpcOperations _operations;

    public RpcOperationsSecurityTests()
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

    [Fact]
    public void InvokeStaticMethod_BlockedAssembly_ThrowsInvalidOperationException()
    {
        // Attempt to call System.IO.File.Exists - should be blocked
        // Blocked assemblies appear as "not found" to untrusted callers
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeStaticMethod("System.IO", "System.IO.File", "Exists", new JsonObject
            {
                ["path"] = "/etc/passwd"
            }));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeStaticMethod_BlockedDiagnosticsAssembly_ThrowsInvalidOperationException()
    {
        // Attempt to call System.Diagnostics.Process.Start - should be blocked
        // Blocked assemblies appear as "not found" to untrusted callers
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.InvokeStaticMethod("System.Diagnostics.Process", "System.Diagnostics.Process", "Start", new JsonObject
            {
                ["fileName"] = "/bin/sh"
            }));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void CreateObject_BlockedAssembly_ThrowsInvalidOperationException()
    {
        // Attempt to create a System.IO.FileInfo - should be blocked
        // Blocked assemblies appear as "not found" to untrusted callers
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.CreateObject("System.IO", "System.IO.FileInfo", new JsonObject
            {
                ["fileName"] = "/etc/passwd"
            }));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void GetStaticProperty_BlockedAssembly_ThrowsInvalidOperationException()
    {
        // Attempt to access Environment.CurrentDirectory - should be blocked
        // Blocked assemblies appear as "not found" to untrusted callers
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.GetStaticProperty("System.Runtime", "System.Environment", "CurrentDirectory"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void SetStaticProperty_BlockedAssembly_ThrowsInvalidOperationException()
    {
        // Attempt to set Environment.CurrentDirectory - should be blocked
        // Blocked assemblies appear as "not found" to untrusted callers
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _operations.SetStaticProperty("System.Runtime", "System.Environment", "CurrentDirectory", "/tmp"));

        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void InvokeMethod_OnRegisteredObject_NotAffectedByAssemblyAllowlist()
    {
        // Instance methods on already-registered objects are allowed
        // because the object was created through legitimate means
        var list = new List<string> { "a", "b", "c" };
        var id = _objectRegistry.Register(list);

        // This should work - we're calling a method on an object we already have
        var result = _operations.InvokeMethod(id, "Contains", new JsonObject { ["item"] = "b" });

        Assert.NotNull(result);
    }
}
