// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Hosting.RemoteHost.CodeGeneration;
using Aspire.Hosting.RemoteHost.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aspire.Hosting.RemoteHost.Tests;

public class RemoteHostBoundaryTests
{
    [Fact]
    public async Task ConfigureServices_PreservesSingletonAndPerClientBoundaries()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["AtsAssemblies:0"] = "Aspire.Hosting";

        var configureServices = typeof(RemoteHostServer).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("RemoteHostServer.ConfigureServices was not found.");

        configureServices.Invoke(null, [builder.Services]);

        using var host = builder.Build();
        var root = host.Services;

        var codeGenerationService = root.GetRequiredService<CodeGenerationService>();
        var catalogProxy = root.GetRequiredService<AtsCatalogProxy>();
        var languageService = root.GetRequiredService<LanguageService>();

        Assert.Same(codeGenerationService, root.GetRequiredService<CodeGenerationService>());
        Assert.Same(catalogProxy, root.GetRequiredService<AtsCatalogProxy>());
        Assert.Same(languageService, root.GetRequiredService<LanguageService>());

        await using var scope1 = root.CreateAsyncScope();
        await using var scope2 = root.CreateAsyncScope();

        var callbacks1 = scope1.ServiceProvider.GetRequiredService<JsonRpcCallbackInvoker>();
        var callbacks2 = scope2.ServiceProvider.GetRequiredService<JsonRpcCallbackInvoker>();
        var sessionProxy1 = scope1.ServiceProvider.GetRequiredService<AtsSessionProxy>();
        var sessionProxy2 = scope2.ServiceProvider.GetRequiredService<AtsSessionProxy>();
        var appHostService1 = scope1.ServiceProvider.GetRequiredService<RemoteAppHostService>();
        var appHostService2 = scope2.ServiceProvider.GetRequiredService<RemoteAppHostService>();

        Assert.NotSame(callbacks1, callbacks2);
        Assert.NotSame(sessionProxy1, sessionProxy2);
        Assert.NotSame(appHostService1, appHostService2);

        Assert.Same(codeGenerationService, scope1.ServiceProvider.GetRequiredService<CodeGenerationService>());
        Assert.Same(catalogProxy, scope2.ServiceProvider.GetRequiredService<AtsCatalogProxy>());
        Assert.Same(languageService, scope2.ServiceProvider.GetRequiredService<LanguageService>());
    }

    [Fact]
    public async Task Proxies_KeepCatalogAndSessionRuntimeInHostingAssembly()
    {
        using var host = CreateHost();
        var root = host.Services;

        var catalogProxy = root.GetRequiredService<AtsCatalogProxy>();
        _ = catalogProxy.GetContext();

        var catalog = GetCatalog(catalogProxy);
        Assert.Equal("Aspire.Hosting.Ats.AtsCatalog", catalog.GetType().FullName);
        Assert.NotSame(typeof(AtsCatalogProxy).Assembly, catalog.GetType().Assembly);

        await using var scope1 = root.CreateAsyncScope();
        await using var scope2 = root.CreateAsyncScope();

        var sessionProxy1 = scope1.ServiceProvider.GetRequiredService<AtsSessionProxy>();
        var sessionProxy2 = scope2.ServiceProvider.GetRequiredService<AtsSessionProxy>();

        var session1 = GetSession(sessionProxy1);
        var session2 = GetSession(sessionProxy2);

        Assert.Equal("Aspire.Hosting.Ats.AtsSession", session1.GetType().FullName);
        Assert.NotSame(typeof(AtsSessionProxy).Assembly, session1.GetType().Assembly);
        Assert.Same(catalog.GetType().Assembly, session1.GetType().Assembly);
        Assert.Same(session1.GetType().Assembly, session2.GetType().Assembly);
        Assert.NotSame(session1, session2);
    }

    [Fact]
    public void RemoteAppHostService_OnlyDependsOnCallbackInvokerSessionProxyAndLogger()
    {
        var constructor = typeof(RemoteAppHostService).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single();

        Assert.Collection(
            constructor.GetParameters(),
            parameter => Assert.Equal(typeof(JsonRpcCallbackInvoker), parameter.ParameterType),
            parameter => Assert.Equal(typeof(AtsSessionProxy), parameter.ParameterType),
            parameter => Assert.Equal(typeof(ILogger<RemoteAppHostService>), parameter.ParameterType));
    }

    [Fact]
    public void CodeGenerationService_OnlyDependsOnCatalogProxyResolverAndLogger()
    {
        var constructor = typeof(CodeGenerationService).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single();

        Assert.Collection(
            constructor.GetParameters(),
            parameter => Assert.Equal(typeof(AtsCatalogProxy), parameter.ParameterType),
            parameter => Assert.Equal(typeof(CodeGeneratorResolver), parameter.ParameterType),
            parameter => Assert.Equal(typeof(ILogger<CodeGenerationService>), parameter.ParameterType));
    }

    [Fact]
    public void LanguageService_OnlyDependsOnResolverAndLogger()
    {
        var constructor = typeof(LanguageService).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single();

        Assert.Collection(
            constructor.GetParameters(),
            parameter => Assert.Equal(typeof(LanguageSupportResolver), parameter.ParameterType),
            parameter => Assert.Equal(typeof(ILogger<LanguageService>), parameter.ParameterType));
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

    private static object GetCatalog(AtsCatalogProxy catalogProxy)
    {
        var stateField = typeof(AtsCatalogProxy).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("AtsCatalogProxy._state was not found.");

        var lazyState = stateField.GetValue(catalogProxy)
            ?? throw new InvalidOperationException("AtsCatalogProxy._state returned null.");

        var state = lazyState.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(lazyState)
            ?? throw new InvalidOperationException("AtsCatalogProxy._state.Value returned null.");

        return state.GetType().GetProperty("Catalog", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(state)
            ?? throw new InvalidOperationException("AtsCatalogProxy catalog state was not found.");
    }

    private static object GetSession(AtsSessionProxy sessionProxy)
    {
        var sessionField = typeof(AtsSessionProxy).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("AtsSessionProxy._session was not found.");

        return sessionField.GetValue(sessionProxy)
            ?? throw new InvalidOperationException("AtsSessionProxy._session returned null.");
    }
}
