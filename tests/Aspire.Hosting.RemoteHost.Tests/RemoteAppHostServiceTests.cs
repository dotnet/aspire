// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.RemoteHost.Ats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class RemoteAppHostServiceTests
{
    [Fact]
    public async Task InvokeCapabilityAsync_UsesSessionProxyAndPreservesHandleShape()
    {
        using var host = CreateHost();
        await using var scope = host.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RemoteAppHostService>();

        var result = await service.InvokeCapabilityAsync("Aspire.Hosting/createBuilderWithOptions", new JsonObject
        {
            ["options"] = new JsonObject
            {
                ["args"] = new JsonArray("--operation", "publish", "--step", "deploy"),
                ["projectDirectory"] = AppContext.BaseDirectory,
                ["appHostFilePath"] = Path.Combine(AppContext.BaseDirectory, "apphost.ts")
            }
        });

        var resultObject = Assert.IsType<JsonObject>(result);
        Assert.NotNull(resultObject["$handle"]);
        Assert.NotNull(resultObject["$type"]);
    }

    [Fact]
    public async Task InvokeCapabilityAsync_ProjectsCapabilityErrorsIntoErrorEnvelope()
    {
        using var host = CreateHost();
        await using var scope = host.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<RemoteAppHostService>();

        var result = await service.InvokeCapabilityAsync("Aspire.Hosting/notARealCapability", null);

        var resultObject = Assert.IsType<JsonObject>(result);
        var error = Assert.IsType<JsonObject>(resultObject["$error"]);
        Assert.Equal(AtsErrorCodes.CapabilityNotFound, error["code"]?.GetValue<string>());
        Assert.Equal("Aspire.Hosting/notARealCapability", error["capability"]?.GetValue<string>());
    }

    [Fact]
    public async Task CancelToken_UsesSessionScopedRegistry()
    {
        using var host = CreateHost();
        await using var scope1 = host.Services.CreateAsyncScope();
        await using var scope2 = host.Services.CreateAsyncScope();

        var service1 = scope1.ServiceProvider.GetRequiredService<RemoteAppHostService>();
        var service2 = scope2.ServiceProvider.GetRequiredService<RemoteAppHostService>();

        Assert.False(service1.CancelToken("ct_1"));
        Assert.False(service2.CancelToken("ct_1"));
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["AtsAssemblies:0"] = "Aspire.Hosting";

        var configureServices = typeof(RemoteHostServer).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RemoteHostServer.ConfigureServices was not found.");

        configureServices.Invoke(null, [builder.Services]);

        return builder.Build();
    }
}
