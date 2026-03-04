// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal static class ResourceDebugSupportExtensions
{
    public static bool SupportsDebugging(this IResource builder, IConfiguration configuration, [NotNullWhen(true)] out SupportsDebuggingAnnotation? supportsDebuggingAnnotation)
    {
        var supportedLaunchConfigurations = GetSupportedLaunchConfigurations(configuration);

        return builder.TryGetLastAnnotation(out supportsDebuggingAnnotation)
            && !string.IsNullOrEmpty(configuration["DEBUG_SESSION_PORT"])
            && ((supportedLaunchConfigurations is null && supportsDebuggingAnnotation.LaunchConfigurationType == "project") // per DCP spec, project resources support debugging if no launch configurations are specified
                || (supportedLaunchConfigurations is not null && supportedLaunchConfigurations.Contains(supportsDebuggingAnnotation.LaunchConfigurationType)));
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
