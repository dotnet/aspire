// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal abstract partial class HotReloadAppModel()
{
    public abstract ValueTask<HotReloadClients?> TryCreateClientsAsync(ILogger clientLogger, ILogger agentLogger, CancellationToken cancellationToken);

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

        context.Logger.Log(MessageDescriptor.ApplicationKind_Default);
        return new DefaultAppModel(projectNode);
    }
}
