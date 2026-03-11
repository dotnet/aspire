// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Aspire.Managed;

/// <summary>
/// Loads the remote host server entry point into an isolated assembly load context.
/// </summary>
internal static class RemoteHostServerInvoker
{
    private const string RemoteHostAssemblyName = "aspire-server";
    private const string RemoteHostServerTypeName = "Aspire.Hosting.RemoteHost.RemoteHostServer";
    private const string RunAsyncMethodName = "RunAsync";

    /// <summary>
    /// Runs the remote host server from an isolated assembly load context.
    /// </summary>
    /// <param name="args">The command line arguments passed to the server.</param>
    internal static async Task RunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var loadContext = new RemoteHostLoadContext(
            typeof(RemoteHostServerInvoker).Assembly,
            ServerSharedAssemblyManifest.GetSharedAssemblyNames(),
            Environment.GetEnvironmentVariable("ASPIRE_INTEGRATION_LIBS_PATH"));
        try
        {
            var remoteHostAssembly = loadContext.LoadFromAssemblyName(new AssemblyName(RemoteHostAssemblyName));
            var remoteHostServerType = remoteHostAssembly.GetType(RemoteHostServerTypeName, throwOnError: true)!;
            var runAsyncMethod = remoteHostServerType.GetMethod(
                RunAsyncMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string[])],
                modifiers: null);

            if (runAsyncMethod?.Invoke(null, [args]) is not Task runTask)
            {
                throw new InvalidOperationException($"Could not invoke {RemoteHostServerTypeName}.{RunAsyncMethodName}(string[]).");
            }

            await runTask.ConfigureAwait(false);
        }
        finally
        {
            loadContext.Unload();
        }
    }
}
