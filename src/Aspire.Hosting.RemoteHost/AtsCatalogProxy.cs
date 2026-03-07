// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Nodes;
using Aspire.Hosting.RemoteHost.Ats;

namespace Aspire.Hosting.RemoteHost;

/// <summary>
/// Reflection proxy over <c>Aspire.Hosting.Ats.AtsCatalog</c> inside the integration load context.
/// </summary>
internal sealed class AtsCatalogProxy
{
    private readonly Lazy<CatalogState> _state;

    public AtsCatalogProxy(AssemblyLoader assemblyLoader)
    {
        _state = new Lazy<CatalogState>(() => CreateState(assemblyLoader));
    }

    public AtsContext GetContext() => _state.Value.Artifacts.Context;

    public object GetIsolatedContext() => _state.Value.IsolatedContext;

    public AtsSessionProxy CreateSession(JsonRpcCallbackInvoker callbackInvoker)
    {
        Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> callbackDelegate = callbackInvoker.InvokeAsync<JsonNode?>;

        var session = _state.Value.CreateSessionMethod.Invoke(_state.Value.Catalog, [callbackDelegate])
            ?? throw new InvalidOperationException("AtsCatalog.CreateSession returned null.");

        return new AtsSessionProxy(session);
    }

    private static CatalogState CreateState(AssemblyLoader assemblyLoader)
    {
        var hostingAssembly = assemblyLoader.GetRequiredAssembly("Aspire.Hosting");
        var catalogType = hostingAssembly.GetType("Aspire.Hosting.Ats.AtsCatalog", throwOnError: true)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsCatalog was not found.");

        var createMethod = catalogType.GetMethod("Create", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsCatalog.Create was not found.");

        var catalog = createMethod.Invoke(null, [assemblyLoader.GetAssemblies()])
            ?? throw new InvalidOperationException("AtsCatalog.Create returned null.");

        var getIsolatedContextMethod = catalogType.GetMethod("GetIsolatedContext", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsCatalog.GetIsolatedContext was not found.");

        var createSessionMethod = catalogType.GetMethod(
            "CreateSession",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: [typeof(Func<string, JsonNode?, CancellationToken, Task<JsonNode?>>)],
            modifiers: null)
            ?? throw new InvalidOperationException("Aspire.Hosting.Ats.AtsCatalog.CreateSession was not found.");

        var isolatedContext = getIsolatedContextMethod.Invoke(catalog, [])
            ?? throw new InvalidOperationException("AtsCatalog.GetIsolatedContext returned null.");

        return new CatalogState
        {
            Catalog = catalog,
            CreateSessionMethod = createSessionMethod,
            IsolatedContext = isolatedContext,
            Artifacts = AtsScannerAdapter.ProjectArtifacts(isolatedContext)
        };
    }

    private sealed class CatalogState
    {
        public required object Catalog { get; init; }

        public required MethodInfo CreateSessionMethod { get; init; }

        public required object IsolatedContext { get; init; }

        public required AtsScanArtifacts Artifacts { get; init; }
    }
}
