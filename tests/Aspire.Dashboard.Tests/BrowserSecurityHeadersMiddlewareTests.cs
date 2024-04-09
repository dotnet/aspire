// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class BrowserSecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Development_AllowExternalFetch()
    {
        // Arrange
        var middleware = CreateMiddleware(environmentName: "Development");
        var httpContext = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.NotEmpty(httpContext.Response.Headers.ContentSecurityPolicy);
        Assert.DoesNotContain("default-src", httpContext.Response.Headers.ContentSecurityPolicy.ToString());
    }

    [Fact]
    public async Task InvokeAsync_Production_DenyExternalFetch()
    {
        // Arrange
        var middleware = CreateMiddleware(environmentName: "Production");
        var httpContext = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.NotEmpty(httpContext.Response.Headers.ContentSecurityPolicy);
        Assert.Contains("default-src", httpContext.Response.Headers.ContentSecurityPolicy.ToString());
    }

    private static BrowserSecurityHeadersMiddleware CreateMiddleware(string environmentName) =>
        new BrowserSecurityHeadersMiddleware(c => Task.CompletedTask, new TestHostEnvironment { EnvironmentName = environmentName });

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "ApplicationName";
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
        public string ContentRootPath { get; set; } = "ContentRootPath";
        public string EnvironmentName { get; set; } = "Development";
    }
}
