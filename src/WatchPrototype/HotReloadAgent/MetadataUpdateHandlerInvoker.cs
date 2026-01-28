// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Finds and invokes metadata update handlers.
/// </summary>
#if NET
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Hot reload is only expected to work when trimming is disabled.")]
[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Hot reload is only expected to work when trimming is disabled.")]
#endif
internal sealed class MetadataUpdateHandlerInvoker(AgentReporter reporter)
{
    internal delegate void ContentUpdateAction(RuntimeStaticAssetUpdate update);
    internal delegate void MetadataUpdateAction(Type[]? updatedTypes);

    internal readonly struct UpdateHandler<TAction>(TAction action, MethodInfo method)
        where TAction : Delegate
    {
        public TAction Action { get; } = action;
        public MethodInfo Method { get; } = method;

        public void ReportInvocation(AgentReporter reporter)
            => reporter.Report(GetHandlerDisplayString(Method), AgentMessageSeverity.Verbose);
    }

    internal sealed class RegisteredActions(
        IReadOnlyList<UpdateHandler<MetadataUpdateAction>> clearCacheHandlers,
        IReadOnlyList<UpdateHandler<MetadataUpdateAction>> updateApplicationHandlers,
        List<UpdateHandler<ContentUpdateAction>> updateContentHandlers)
    {
        public void MetadataUpdated(AgentReporter reporter, Type[] updatedTypes)
        {
            foreach (var handler in clearCacheHandlers)
            {
                handler.ReportInvocation(reporter);
                handler.Action(updatedTypes);
            }

            foreach (var handler in updateApplicationHandlers)
            {
                handler.ReportInvocation(reporter);
                handler.Action(updatedTypes);
            }
        }

        public void UpdateContent(AgentReporter reporter, RuntimeStaticAssetUpdate update)
        {
            foreach (var handler in updateContentHandlers)
            {
                handler.ReportInvocation(reporter);
                handler.Action(update);
            }
        }

        /// <summary>
        /// For testing.
        /// </summary>
        internal IEnumerable<UpdateHandler<MetadataUpdateAction>> ClearCacheHandlers => clearCacheHandlers;

        /// <summary>
        /// For testing.
        /// </summary>
        internal IEnumerable<UpdateHandler<MetadataUpdateAction>> UpdateApplicationHandlers => updateApplicationHandlers;

        /// <summary>
        /// For testing.
        /// </summary>
        internal IEnumerable<UpdateHandler<ContentUpdateAction>> UpdateContentHandlers => updateContentHandlers;
    }

    private const string ClearCacheHandlerName = "ClearCache";
    private const string UpdateApplicationHandlerName = "UpdateApplication";
    private const string UpdateContentHandlerName = "UpdateContent";
    private const BindingFlags HandlerMethodBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

    private static readonly Type[] s_contentUpdateSignature = [typeof(string), typeof(bool), typeof(string), typeof(byte[])];
    private static readonly Type[] s_metadataUpdateSignature = [typeof(Type[])];

    private RegisteredActions? _actions;

    /// <summary>
    /// Call when a new assembly is loaded.
    /// </summary>
    internal void Clear()
        => Interlocked.Exchange(ref _actions, null);

    private RegisteredActions GetActions()
    {
        // Defer discovering metadata updata handlers until after hot reload deltas have been applied.
        // This should give enough opportunity for AppDomain.GetAssemblies() to be sufficiently populated.
        var actions = _actions;
        if (actions == null)
        {
            Interlocked.CompareExchange(ref _actions, GetUpdateHandlerActions(), null);
            actions = _actions;
        }

        return actions;
    }

    /// <summary>
    /// Invokes all registered metadata update handlers.
    /// </summary>
    internal void MetadataUpdated(Type[] updatedTypes)
    {
        try
        {
            reporter.Report("Invoking metadata update handlers.", AgentMessageSeverity.Verbose);

            GetActions().MetadataUpdated(reporter, updatedTypes);
        }
        catch (Exception e)
        {
            reporter.Report(e.ToString(), AgentMessageSeverity.Warning);
        }
    }

    /// <summary>
    /// Invokes all registered content update handlers.
    /// </summary>
    internal void ContentUpdated(RuntimeStaticAssetUpdate update)
    {
        try
        {
            reporter.Report("Invoking content update handlers.", AgentMessageSeverity.Verbose);

            GetActions().UpdateContent(reporter, update);
        }
        catch (Exception e)
        {
            reporter.Report(e.ToString(), AgentMessageSeverity.Warning);
        }
    }

    private IEnumerable<Type> GetHandlerTypes()
    {
        // We need to execute MetadataUpdateHandlers in a well-defined order. For v1, the strategy that is used is to topologically
        // sort assemblies so that handlers in a dependency are executed before the dependent (e.g. the reflection cache action
        // in System.Private.CoreLib is executed before System.Text.Json clears its own cache.)
        // This would ensure that caches and updates more lower in the application stack are up to date
        // before ones higher in the stack are recomputed.
        var sortedAssemblies = TopologicalSort(AppDomain.CurrentDomain.GetAssemblies());

        foreach (var assembly in sortedAssemblies)
        {
            foreach (var attr in TryGetCustomAttributesData(assembly))
            {
                // Look up the attribute by name rather than by type. This would allow netstandard targeting libraries to
                // define their own copy without having to cross-compile.
                if (attr.AttributeType.FullName != "System.Reflection.Metadata.MetadataUpdateHandlerAttribute")
                {
                    continue;
                }

                IList<CustomAttributeTypedArgument> ctorArgs = attr.ConstructorArguments;
                if (ctorArgs.Count != 1 ||
                    ctorArgs[0].Value is not Type handlerType)
                {
                    reporter.Report($"'{attr}' found with invalid arguments.", AgentMessageSeverity.Warning);
                    continue;
                }

                yield return handlerType;
            }
        }
    }

    public RegisteredActions GetUpdateHandlerActions()
        => GetUpdateHandlerActions(GetHandlerTypes());

    /// <summary>
    /// Internal for testing.
    /// </summary>
    internal RegisteredActions GetUpdateHandlerActions(IEnumerable<Type> handlerTypes)
    {
        var clearCacheHandlers = new List<UpdateHandler<MetadataUpdateAction>>();
        var applicationUpdateHandlers = new List<UpdateHandler<MetadataUpdateAction>>();
        var contentUpdateHandlers = new List<UpdateHandler<ContentUpdateAction>>();

        foreach (var handlerType in handlerTypes)
        {
            bool methodFound = false;

            if (GetMetadataUpdateMethod(handlerType, ClearCacheHandlerName) is MethodInfo clearCache)
            {
                clearCacheHandlers.Add(CreateMetadataUpdateAction(clearCache));
                methodFound = true;
            }

            if (GetMetadataUpdateMethod(handlerType, UpdateApplicationHandlerName) is MethodInfo updateApplication)
            {
                applicationUpdateHandlers.Add(CreateMetadataUpdateAction(updateApplication));
                methodFound = true;
            }

            if (GetContentUpdateMethod(handlerType, UpdateContentHandlerName) is MethodInfo updateContent)
            {
                contentUpdateHandlers.Add(CreateContentUpdateAction(updateContent));
                methodFound = true;
            }

            if (!methodFound)
            {
                reporter.Report(
                    $"Expected to find a static method '{ClearCacheHandlerName}', '{UpdateApplicationHandlerName}' or '{UpdateContentHandlerName}' on type '{handlerType.AssemblyQualifiedName}' but neither exists.",
                    AgentMessageSeverity.Warning);
            }
        }

        return new RegisteredActions(clearCacheHandlers, applicationUpdateHandlers, contentUpdateHandlers);

        UpdateHandler<MetadataUpdateAction> CreateMetadataUpdateAction(MethodInfo method)
        {
            var action = (MetadataUpdateAction)method.CreateDelegate(typeof(MetadataUpdateAction));
            return new(types =>
            {
                try
                {
                    action(types);
                }
                catch (Exception e)
                {
                    ReportException(e, method);
                }
            }, method);
        }

        UpdateHandler<ContentUpdateAction> CreateContentUpdateAction(MethodInfo method)
        {
            var action = (Action<string, bool, string, byte[]>)method.CreateDelegate(typeof(Action<string, bool, string, byte[]>));
            return new(update =>
            {
                try
                {
                    action(update.AssemblyName, update.IsApplicationProject, update.RelativePath, update.Contents);
                }
                catch (Exception e)
                {
                    ReportException(e, method);
                }
            }, method);
        }

        void ReportException(Exception e, MethodInfo method)
            => reporter.Report($"Exception from '{GetHandlerDisplayString(method)}': {e}", AgentMessageSeverity.Warning);

        MethodInfo? GetMetadataUpdateMethod(Type handlerType, string name)
        {
            if (handlerType.GetMethod(name, HandlerMethodBindingFlags, binder: null, s_metadataUpdateSignature, modifiers: null) is MethodInfo updateMethod &&
                updateMethod.ReturnType == typeof(void))
            {
                return updateMethod;
            }

            ReportSignatureMismatch(handlerType, name);
            return null;
        }

        MethodInfo? GetContentUpdateMethod(Type handlerType, string name)
        {
            if (handlerType.GetMethod(name, HandlerMethodBindingFlags, binder: null, s_contentUpdateSignature, modifiers: null) is MethodInfo updateMethod &&
                updateMethod.ReturnType == typeof(void))
            {
                return updateMethod;
            }

            ReportSignatureMismatch(handlerType, name);
            return null;
        }

        void ReportSignatureMismatch(Type handlerType, string name)
        {
            foreach (MethodInfo method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name == name)
                {
                    reporter.Report($"Type '{handlerType}' has method '{method}' that does not match the required signature.", AgentMessageSeverity.Warning);
                    break;
                }
            }
        }
    }

    private static string GetHandlerDisplayString(MethodInfo method)
        => $"{method.DeclaringType!.FullName}.{method.Name}";

    private IList<CustomAttributeData> TryGetCustomAttributesData(Assembly assembly)
    {
        try
        {
            return assembly.GetCustomAttributesData();
        }
        catch (Exception e)
        {
            // In cross-platform scenarios, such as debugging in VS through WSL, Roslyn
            // runs on Windows, and the agent runs on Linux. Assemblies accessible to Windows
            // may not be available or loaded on linux (such as WPF's assemblies).
            // In such case, we can ignore the assemblies and continue enumerating handlers for
            // the rest of the assemblies of current domain.
            reporter.Report($"'{assembly.FullName}' is not loaded ({e.Message})", AgentMessageSeverity.Verbose);
            return [];
        }
    }

    /// <summary>
    /// Internal for testing.
    /// </summary>
    internal static List<Assembly> TopologicalSort(Assembly[] assemblies)
    {
        var sortedAssemblies = new List<Assembly>(assemblies.Length);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in assemblies)
        {
            Visit(assemblies, assembly, sortedAssemblies, visited);
        }

        static void Visit(Assembly[] assemblies, Assembly assembly, List<Assembly> sortedAssemblies, HashSet<string> visited)
        {
            string? assemblyIdentifier;

            try
            {
                assemblyIdentifier = assembly.GetName().Name;
            }
            catch
            {
                return;
            }

            if (assemblyIdentifier == null || !visited.Add(assemblyIdentifier))
            {
                return;
            }

            AssemblyName[] referencedAssemblies;
            try
            {
                referencedAssemblies = assembly.GetReferencedAssemblies();
            }
            catch
            {
                referencedAssemblies = [];
            }

            foreach (var dependencyName in referencedAssemblies)
            {
                var dependency = Array.Find(assemblies, a =>
                {
                    try
                    {
                        return string.Equals(a.GetName().Name, dependencyName.Name, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return false;
                    }
                });

                if (dependency is not null)
                {
                    Visit(assemblies, dependency, sortedAssemblies, visited);
                }
            }

            sortedAssemblies.Add(assembly);
        }

        return sortedAssemblies;
    }
}
