// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract partial class HotReloadAppModel()
{
    public abstract ValueTask<HotReloadClients> CreateClientsAsync(ILogger clientLogger, ILogger agentLogger, CancellationToken cancellationToken);

    protected static string GetInjectedAssemblyPath(string targetFramework, string assemblyName)
        => Path.Combine(Path.GetDirectoryName(typeof(HotReloadAppModel).Assembly.Location)!, "hotreload", targetFramework, assemblyName + ".dll");

    public static string GetStartupHookPath(ProjectGraphNode project)
    {
        var hookTargetFramework = project.GetTargetFrameworkVersion() is { Major: >= 10 } ? "net10.0" : "net6.0";
        return GetInjectedAssemblyPath(hookTargetFramework, "Microsoft.Extensions.DotNetDeltaApplier");
    }

    public static HotReloadAppModel InferFromProject(DotNetWatchContext context, ProjectGraphNode projectNode)
    {
        var capabilities = projectNode.GetCapabilities();

        if (capabilities.Contains(ProjectCapability.WebAssembly))
        {
            context.Logger.Log(MessageDescriptor.ApplicationKind_BlazorWebAssembly);
            return new BlazorWebAssemblyAppModel(context, clientProject: projectNode);
        }

        if (capabilities.Contains(ProjectCapability.AspNetCore))
        {
            if (projectNode.GetDescendantsAndSelf().FirstOrDefault(static p => p.GetCapabilities().Contains(ProjectCapability.WebAssembly)) is { } clientProject)
            {
                context.Logger.Log(MessageDescriptor.ApplicationKind_BlazorHosted, projectNode.ProjectInstance.FullPath, clientProject.ProjectInstance.FullPath);
                return new BlazorWebAssemblyHostedAppModel(context, clientProject: clientProject, serverProject: projectNode);
            }

            context.Logger.Log(MessageDescriptor.ApplicationKind_WebApplication);
            return new WebServerAppModel(context, serverProject: projectNode);
        }

        if (capabilities.Contains(ProjectCapability.HotReloadWebSockets))
        {
            context.Logger.Log(MessageDescriptor.ApplicationKind_WebSockets);
            return new MobileAppModel(context, projectNode);
        }

        context.Logger.Log(MessageDescriptor.ApplicationKind_Default);
        return new DefaultAppModel(projectNode);
    }

    /// <summary>
    /// True if a managed code agent can be injected into the target process.
    /// The agent is injected either via dotnet startup hook, or via web server middleware for WASM clients.
    /// </summary>
    internal static bool IsManagedAgentSupported(ProjectGraphNode project, ILogger logger)
    {
        if (!project.IsNetCoreApp(Versions.Version6_0))
        {
            LogWarning("target framework is older than 6.0");
            return false;
        }

        // If property is not specified startup hook is enabled:
        // https://github.com/dotnet/runtime/blob/4b0b7238ba021b610d3963313b4471517108d2bc/src/libraries/System.Private.CoreLib/src/System/StartupHookProvider.cs#L22
        // Startup hooks are not used for WASM projects.
        //
        // TODO: Remove once implemented: https://github.com/dotnet/runtime/issues/123778
        if (!project.ProjectInstance.GetBooleanPropertyValue(PropertyNames.StartupHookSupport, defaultValue: true) &&
            !project.GetCapabilities().Contains(ProjectCapability.WebAssembly))
        {
            // Report which property is causing lack of support for startup hooks:
            var (propertyName, propertyValue) =
                project.ProjectInstance.GetBooleanPropertyValue(PropertyNames.PublishAot)
                ? (PropertyNames.PublishAot, true)
                : project.ProjectInstance.GetBooleanPropertyValue(PropertyNames.PublishTrimmed)
                ? (PropertyNames.PublishTrimmed, true)
                : (PropertyNames.StartupHookSupport, false);

            LogWarning(string.Format("'{0}' property is '{1}'", propertyName, propertyValue));
            return false;
        }

        return true;

        void LogWarning(string reason)
            => logger.Log(MessageDescriptor.ProjectDoesNotSupportHotReload, reason);
    }
}
