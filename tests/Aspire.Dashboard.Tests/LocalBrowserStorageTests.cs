// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Dashboard.Model.BrowserStorage;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class LocalBrowserStorageTests
{
    [Theory]
    [InlineData(123, "123")]
    [InlineData("Hello world", @"""Hello world""")]
    [InlineData(null, "null")]
    public async Task SetUnprotectedAsync_JSInvokedWithJson(object? value, string result)
    {
        // Arrange
        string? identifier = null;
        object?[]? args = null;

        var testJsonRuntime = new TestJSRuntime();
        testJsonRuntime.OnInvoke = r =>
        {
            (identifier, args) = r;
            return default;
        };
        var localStorage = CreateBrowserLocalStorage(testJsonRuntime);

        // Act
        await localStorage.SetUnprotectedAsync("MyKey", value).DefaultTimeout();

        // Assert
        Assert.Equal("localStorage.setItem", identifier);
        Assert.NotNull(args);
        Assert.Equal("MyKey", args[0]);
        Assert.Equal(result, args[1]);
    }

    [Fact]
    public async Task GetUnprotectedAsync_HasValue_Success()
    {
        // Arrange
        string? identifier = null;
        object?[]? args = null;

        var testJsonRuntime = new TestJSRuntime();
        testJsonRuntime.OnInvoke = r =>
        {
            (identifier, args) = r;
            return "123";
        };
        var localStorage = CreateBrowserLocalStorage(testJsonRuntime);

        // Act
        var result = await localStorage.GetUnprotectedAsync<int>("MyKey").DefaultTimeout();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(123, result.Value);
        Assert.Equal("localStorage.getItem", identifier);
        Assert.NotNull(args);
        Assert.Equal("MyKey", args[0]);
    }

    [Fact]
    public async Task GetUnprotectedAsync_NoValue_Failure()
    {
        // Arrange
        string? identifier = null;
        object?[]? args = null;

        var testJsonRuntime = new TestJSRuntime();
        testJsonRuntime.OnInvoke = r =>
        {
            (identifier, args) = r;
            return default;
        };
        var localStorage = CreateBrowserLocalStorage(testJsonRuntime);

        // Act
        var result = await localStorage.GetUnprotectedAsync<int>("MyKey").DefaultTimeout();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("localStorage.getItem", identifier);
        Assert.NotNull(args);
        Assert.Equal("MyKey", args[0]);
    }

    [Fact]
    public async Task GetUnprotectedAsync_InvalidValue_Failure()
    {
        // Arrange
        string? identifier = null;
        object?[]? args = null;

        var testJsonRuntime = new TestJSRuntime();
        testJsonRuntime.OnInvoke = r =>
        {
            (identifier, args) = r;
            return "One";
        };
        var localStorage = CreateBrowserLocalStorage(testJsonRuntime);

        // Act
        var result = await localStorage.GetUnprotectedAsync<int>("MyKey").DefaultTimeout();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("localStorage.getItem", identifier);
        Assert.NotNull(args);
        Assert.Equal("MyKey", args[0]);
    }

    private static LocalBrowserStorage CreateBrowserLocalStorage(TestJSRuntime testJsonRuntime)
    {
        return new LocalBrowserStorage(
            testJsonRuntime,
            new ProtectedLocalStorage(testJsonRuntime, new TestDataProtector()),
            NullLogger<LocalBrowserStorage>.Instance);
    }

    private sealed class TestJSRuntime : IJSRuntime
    {
        public Func<(string Identifier, object?[]? Args), object?>? OnInvoke { get; set; }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args)
        {
            if (OnInvoke?.Invoke((identifier, args)) is TValue result)
            {
                return ValueTask.FromResult(result);
            }
            return default;
        }

        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (OnInvoke?.Invoke((identifier, args)) is TValue result)
            {
                return ValueTask.FromResult(result);
            }
            return default;
        }
    }

    private sealed class TestDataProtector : IDataProtector
    {
        public IDataProtector CreateProtector(string purpose)
        {
            throw new NotImplementedException();
        }

        public byte[] Protect(byte[] plaintext)
        {
            throw new NotImplementedException();
        }

        public byte[] Unprotect(byte[] protectedData)
        {
            throw new NotImplementedException();
        }
    }
}
