// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace Microsoft.DotNet.HotReload;

#if NET
[System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
#endif
internal sealed class HotReloadAgent : IDisposable, IHotReloadAgent
{
    private const string MetadataUpdaterTypeName = "System.Reflection.Metadata.MetadataUpdater";
    private const string ApplyUpdateMethodName = "ApplyUpdate";
    private const string GetCapabilitiesMethodName = "GetCapabilities";

    private delegate void ApplyUpdateDelegate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta);

    public AgentReporter Reporter { get; } = new();

    private readonly ConcurrentDictionary<Guid, List<RuntimeManagedCodeUpdate>> _moduleUpdates = new();
    private readonly ConcurrentDictionary<Assembly, Assembly> _appliedAssemblies = new();
    private readonly ApplyUpdateDelegate? _applyUpdate;
    private readonly string? _capabilities;
    private readonly MetadataUpdateHandlerInvoker _metadataUpdateHandlerInvoker;

    // handler to install on first managed update:
    private Func<AssemblyLoadContext, AssemblyName, Assembly?>? _assemblyResolvingHandlerToInstall;
    private Func<AssemblyLoadContext, AssemblyName, Assembly?>? _installedAssemblyResolvingHandler;

    // handler to install to HotReloadException.Created:
    private Action<int, string>? _hotReloadExceptionCreateHandler;

    public HotReloadAgent(
        Func<AssemblyLoadContext, AssemblyName, Assembly?>? assemblyResolvingHandler,
        Action<int, string>? hotReloadExceptionCreateHandler)
    {
        _metadataUpdateHandlerInvoker = new(Reporter);
        _assemblyResolvingHandlerToInstall = assemblyResolvingHandler;
        _hotReloadExceptionCreateHandler = hotReloadExceptionCreateHandler;
        GetUpdaterMethodsAndCapabilities(out _applyUpdate, out _capabilities);

        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        AssemblyLoadContext.Default.Resolving -= _installedAssemblyResolvingHandler;
    }

    private void GetUpdaterMethodsAndCapabilities(out ApplyUpdateDelegate? applyUpdate, out string? capabilities)
    {
        applyUpdate = null;
        capabilities = null;

        var metadataUpdater = Type.GetType(MetadataUpdaterTypeName + ", System.Runtime.Loader", throwOnError: false);
        if (metadataUpdater == null)
        {
            Reporter.Report($"Type not found: {MetadataUpdaterTypeName}", AgentMessageSeverity.Error);
            return;
        }

        var applyUpdateMethod = metadataUpdater.GetMethod(ApplyUpdateMethodName, BindingFlags.Public | BindingFlags.Static, binder: null, [typeof(Assembly), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>), typeof(ReadOnlySpan<byte>)], modifiers: null);
        if (applyUpdateMethod == null)
        {
            Reporter.Report($"{MetadataUpdaterTypeName}.{ApplyUpdateMethodName} not found.", AgentMessageSeverity.Error);
            return;
        }

        applyUpdate = (ApplyUpdateDelegate)applyUpdateMethod.CreateDelegate(typeof(ApplyUpdateDelegate));

        var getCapabilities = metadataUpdater.GetMethod(GetCapabilitiesMethodName, BindingFlags.NonPublic | BindingFlags.Static, binder: null, Type.EmptyTypes, modifiers: null);
        if (getCapabilities == null)
        {
            Reporter.Report($"{MetadataUpdaterTypeName}.{GetCapabilitiesMethodName} not found.", AgentMessageSeverity.Error);
            return;
        }

        try
        {
            capabilities = getCapabilities.Invoke(obj: null, parameters: null) as string;
        }
        catch (Exception e)
        {
            Reporter.Report($"Error retrieving capabilities: {e.Message}", AgentMessageSeverity.Error);
        }
    }

    public string Capabilities => _capabilities ?? string.Empty;

    private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs)
    {
        _metadataUpdateHandlerInvoker.Clear();

        var loadedAssembly = eventArgs.LoadedAssembly;
        var moduleId = TryGetModuleId(loadedAssembly);
        if (moduleId is null)
        {
            return;
        }

        if (_moduleUpdates.TryGetValue(moduleId.Value, out var moduleUpdate) && _appliedAssemblies.TryAdd(loadedAssembly, loadedAssembly))
        {
            // A delta for this specific Module exists and we haven't called ApplyUpdate on this instance of Assembly as yet.
            ApplyDeltas(loadedAssembly, moduleUpdate);
        }
    }

    public void ApplyManagedCodeUpdates(IEnumerable<RuntimeManagedCodeUpdate> updates)
    {
        Debug.Assert(Capabilities.Length > 0);
        Debug.Assert(_applyUpdate != null);

        var handler = Interlocked.Exchange(ref _assemblyResolvingHandlerToInstall, null);
        if (handler != null)
        {
            AssemblyLoadContext.Default.Resolving += handler;
            _installedAssemblyResolvingHandler = handler;
        }

        foreach (var update in updates)
        {
            if (update.MetadataDelta.Length == 0)
            {
                // When the debugger is attached the delta is empty.
                // The client only calls to trigger metadata update handlers.
                continue;
            }

            Reporter.Report($"Applying updates to module {update.ModuleId}.", AgentMessageSeverity.Verbose);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TryGetModuleId(assembly) is Guid moduleId && moduleId == update.ModuleId)
                {
                    _applyUpdate(assembly, update.MetadataDelta, update.ILDelta, update.PdbDelta);
                }
            }

            // Additionally stash the deltas away so it may be applied to assemblies loaded later.
            var cachedModuleUpdates = _moduleUpdates.GetOrAdd(update.ModuleId, static _ => []);
            cachedModuleUpdates.Add(update);
        }

        var updatedTypes = GetMetadataUpdateTypes(updates);

        InstallHotReloadExceptionCreatedHandler(updatedTypes);

        _metadataUpdateHandlerInvoker.MetadataUpdated(updatedTypes);

        Reporter.Report("Updates applied.", AgentMessageSeverity.Verbose);
    }

    private void InstallHotReloadExceptionCreatedHandler(Type[] types)
    {
        if (_hotReloadExceptionCreateHandler is null)
        {
            // already installed or not available
            return;
        }

        var exceptionType = types.FirstOrDefault(static t => t.FullName == "System.Runtime.CompilerServices.HotReloadException");
        if (exceptionType == null)
        {
            return;
        }

        var handler = Interlocked.Exchange(ref _hotReloadExceptionCreateHandler, null);
        if (handler == null)
        {
            // already installed or not available
            return;
        }

        // HotReloadException has a private static field Action<Exception> Created, unless emitted by previous versions of the compiler:
        // See https://github.com/dotnet/roslyn/blob/06f2643e1268e4a7fcdf1221c052f9c8cce20b60/src/Compilers/CSharp/Portable/Symbols/Synthesized/SynthesizedHotReloadExceptionSymbol.cs#L29
        var createdField = exceptionType.GetField("Created", BindingFlags.Static | BindingFlags.NonPublic);
        var codeField = exceptionType.GetField("Code", BindingFlags.Public | BindingFlags.Instance);
        if (createdField == null || codeField == null)
        {
            Reporter.Report($"Failed to install HotReloadException handler: not supported by the compiler", AgentMessageSeverity.Verbose);
            return;
        }

        try
        {
            createdField.SetValue(null, new Action<Exception>(e =>
            {
                try
                {
                    handler(codeField.GetValue(e) is int code ? code : 0, e.Message);
                }
                catch
                {
                    // do not crash the app
                }
            }));
        }
        catch (Exception e)
        {
            Reporter.Report($"Failed to install HotReloadException handler: {e.Message}", AgentMessageSeverity.Verbose);
            return;
        }

        Reporter.Report($"HotReloadException handler installed.", AgentMessageSeverity.Verbose);
    }

    private Type[] GetMetadataUpdateTypes(IEnumerable<RuntimeManagedCodeUpdate> updates)
    {
        List<Type>? types = null;

        foreach (var update in updates)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => TryGetModuleId(assembly) is Guid moduleId && moduleId == update.ModuleId);
            if (assembly is null)
            {
                continue;
            }

            foreach (var updatedType in update.UpdatedTypes)
            {
                // Must be a TypeDef.
                Debug.Assert(updatedType >> 24 == 0x02);

                // The type has to be in the manifest module since Hot Reload does not support multi-module assemblies:
                try
                {
                    var type = assembly.ManifestModule.ResolveType(updatedType);
                    types ??= [];
                    types.Add(type);
                }
                catch (Exception e)
                {
                    Reporter.Report($"Failed to load type 0x{updatedType:X8}: {e.Message}", AgentMessageSeverity.Warning);
                }
            }
        }

        return types?.ToArray() ?? Type.EmptyTypes;
    }

    private void ApplyDeltas(Assembly assembly, IReadOnlyList<RuntimeManagedCodeUpdate> updates)
    {
        Debug.Assert(_applyUpdate != null);

        try
        {
            foreach (var update in updates)
            {
                _applyUpdate(assembly, update.MetadataDelta, update.ILDelta, update.PdbDelta);
            }

            Reporter.Report("Updates applied.", AgentMessageSeverity.Verbose);
        }
        catch (Exception ex)
        {
            Reporter.Report(ex.ToString(), AgentMessageSeverity.Warning);
        }
    }

    private static Guid? TryGetModuleId(Assembly loadedAssembly)
    {
        try
        {
            return loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId;
        }
        catch
        {
            // Assembly.Modules might throw. See https://github.com/dotnet/aspnetcore/issues/33152
        }

        return default;
    }

    /// <summary>
    /// Applies the content update.
    /// </summary>
    public void ApplyStaticAssetUpdate(RuntimeStaticAssetUpdate update)
    {
        _metadataUpdateHandlerInvoker.ContentUpdated(update);
    }

    /// <summary>
    /// Clear any hot-reload specific environment variables. This prevents child processes from being
    /// affected by the current app's hot reload settings. See https://github.com/dotnet/runtime/issues/58000
    /// </summary>
    public static void ClearHotReloadEnvironmentVariables(Type startupHookType)
    {
        var startupHooks = Environment.GetEnvironmentVariable(AgentEnvironmentVariables.DotNetStartupHooks);
        if (!string.IsNullOrEmpty(startupHooks))
        {
            Environment.SetEnvironmentVariable(AgentEnvironmentVariables.DotNetStartupHooks,
                RemoveCurrentAssembly(startupHookType, startupHooks));
        }

        Environment.SetEnvironmentVariable(AgentEnvironmentVariables.DotNetWatchHotReloadNamedPipeName, null);
        Environment.SetEnvironmentVariable(AgentEnvironmentVariables.HotReloadDeltaClientLogMessages, null);
    }

    // internal for testing
    internal static string RemoveCurrentAssembly(Type startupHookType, string environment)
    {
        Debug.Assert(!string.IsNullOrEmpty(environment), $"{nameof(environment)} must be set");

        var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        var assemblyLocation = startupHookType.Assembly.Location;
        var updatedValues = environment.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(e => !string.Equals(e, assemblyLocation, comparison));

        return string.Join(Path.PathSeparator, updatedValues);
    }
}
