// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

internal static class ConsoleLogsConfigurationExtensions
{
    internal static IResourceBuilder<T> ConfigureConsoleLogs<T>(this IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment((context) =>
        {
            if (context.ExecutionContext.IsPublishMode)
            {
                return;
            }
            // Enable ANSI Control Sequences for colors in Output Redirection
            context.EnvironmentVariables["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "true";

            // Enable Simple Console Logger Formatting
            context.EnvironmentVariables["LOGGING__CONSOLE__FORMATTERNAME"] = "simple";
        });
    }
}
