// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal static class ResourceDebugSupportExtensions
{
    private const string DebugSessionPort = "DEBUG_SESSION_PORT";

    /// <summary>
    /// Adds support for debugging the resource in VS Code when running in an extension host.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="launchConfigurationProducer">Launch configuration producer for the resource.</param>
    /// <param name="launchConfigurationType">The type of the resource.</param>
    /// <param name="argsCallback">Optional callback to add or modify command line arguments when running in an extension host. Useful if the entrypoint is usually provided as an argument to the resource executable.</param>
    internal static IResourceBuilder<T> WithDebugSupport<T, TLaunchConfiguration>(this IResourceBuilder<T> builder, Func<LaunchConfigurationProducerOptions, TLaunchConfiguration> launchConfigurationProducer, string launchConfigurationType, Action<CommandLineArgsCallbackContext>? argsCallback = null)
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
                if (resourceWithArgs.Resource.SupportsDebugging(builder.ApplicationBuilder.Configuration, out _) && argsCallback is not null)
                {
                    argsCallback(ctx);
                }
            });
        }

        return builder.WithAnnotation(SupportsDebuggingAnnotation.Create<TLaunchConfiguration>(launchConfigurationType, launchConfigurationProducer));
    }

    internal static bool SupportsDebugging(this IResourceBuilder<IResourceWithArgs> builder, IConfiguration configuration)
    {
        var supportedLaunchConfigurations = GetSupportedLaunchConfigurations(configuration);

        return builder.Resource.TryGetLastAnnotation<SupportsDebuggingAnnotation>(out var supportsDebuggingAnnotation)
            && !string.IsNullOrEmpty(configuration[DebugSessionPort])
            && supportedLaunchConfigurations is not null
            && supportedLaunchConfigurations.Contains(supportsDebuggingAnnotation.LaunchConfigurationType);
    }

    internal static string[]? GetSupportedLaunchConfigurations(this IConfiguration configuration)
    {
        try
        {
            if (configuration[KnownConfigNames.DebugSessionInfo] is { } debugSessionInfoJson && JsonSerializer.Deserialize<RunSessionInfo>(debugSessionInfoJson) is { } debugSessionInfo)
            {
                return debugSessionInfo.SupportedLaunchConfigurations;
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    internal sealed class RunSessionInfo
    {
        [JsonPropertyName("protocols_supported")]
        public required string[] ProtocolsSupported { get; set; }

        [JsonPropertyName("supported_launch_configurations")]
        public string[]? SupportedLaunchConfigurations { get; set; }
    }
}
#pragma warning restore ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
