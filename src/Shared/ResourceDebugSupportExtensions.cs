// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

internal static class ResourceDebugSupportExtensions
{
    /// <summary>
    /// Adds support for debugging the resource in VS Code when running in an extension host.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="launchConfigurationProducer">Launch configuration producer for the resource.</param>
    /// <param name="launchConfigurationType">The type of the resource.</param>
    /// <param name="argsCallback">Optional callback to add or modify command line arguments when running in an extension host. Useful if the entrypoint is usually provided as an argument to the resource executable.</param>
    [Experimental("ASPIREEXTENSION001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    internal static IResourceBuilder<T> WithDebugSupport<T, TLaunchConfiguration>(this IResourceBuilder<T> builder, Func<string, TLaunchConfiguration> launchConfigurationProducer, string launchConfigurationType, Action<CommandLineArgsCallbackContext>? argsCallback = null)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(launchConfigurationProducer);

        if (!builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            return builder;
        }

        if (builder is IResourceBuilder<IResourceWithArgs> resourceWithArgs)
        {
            resourceWithArgs.WithArgs(async ctx =>
            {
                var config = ctx.ExecutionContext.ServiceProvider.GetRequiredService<IConfiguration>();
                if (resourceWithArgs.SupportsDebugging(config) && argsCallback is not null)
                {
                    argsCallback(ctx);
                }
            });
        }

        return builder.WithAnnotation(SupportsDebuggingAnnotation.Create(launchConfigurationType, launchConfigurationProducer));
    }
}
