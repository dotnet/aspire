// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.VirtualShell;
using Aspire.Hosting.VirtualShell.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding VirtualShell services to the dependency injection container.
/// </summary>
public static class VirtualShellExtensions
{
    /// <summary>
    /// Adds VirtualShell services to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVirtualShell(this IServiceCollection services)
    {
        services.AddSingleton<ICommandLineParser, CommandLineParser>();
        services.AddSingleton<IExecutableResolver, ExecutableResolver>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddTransient<IVirtualShell>(sp =>
            new VirtualShell(
                sp.GetRequiredService<ICommandLineParser>(),
                sp.GetRequiredService<IExecutableResolver>(),
                sp.GetRequiredService<IProcessRunner>()));

        return services;
    }
}
