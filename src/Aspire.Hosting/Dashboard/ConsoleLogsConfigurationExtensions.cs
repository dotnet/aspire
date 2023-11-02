// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.ConsoleLogs;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Dashboard;

internal static class ConsoleLogsConfigurationExtensions
{
    internal static IResourceBuilder<T> ConfigureConsoleLogs<T>(this IResourceBuilder<T> builder) where T : IResourceWithEnvironment
    {
        return builder.WithEnvironment((context) =>
        {
            if (context.PublisherName == "manifest")
            {
                return;
            }
            // Enable ANSI Control Sequences for colors in Output Redirection
            context.EnvironmentVariables["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "true";

            // Enable Simple Console Logger Formatting with a UTC timestamp similar to RFC3339Nano that Docker generates
            context.EnvironmentVariables["LOGGING__CONSOLE__FORMATTERNAME"] = "simple";
            context.EnvironmentVariables["LOGGING__CONSOLE__FORMATTEROPTIONS__TIMESTAMPFORMAT"] = $"{TimestampParser.DisplayFormat} ";
        });
    }
}
