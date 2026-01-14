// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting.Utils;

#pragma warning disable ASPIREEXTENSION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal static class ExtensionUtils
{
    public static bool SupportsDebugging(this IResourceBuilder<IResourceWithArgs> builder, IConfiguration configuration)
    {
        var supportedLaunchConfigurations = GetSupportedLaunchConfigurations(configuration);

        return builder.Resource.TryGetLastAnnotation<SupportsDebuggingAnnotation>(out var supportsDebuggingAnnotation)
            && !string.IsNullOrEmpty(configuration[DcpExecutor.DebugSessionPortVar])
            && supportedLaunchConfigurations is not null
            && supportedLaunchConfigurations.Contains(supportsDebuggingAnnotation.LaunchConfigurationType);
    }

    public static string[]? GetSupportedLaunchConfigurations(IConfiguration configuration)
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
}
