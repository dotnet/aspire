// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Aspire.Hosting.Utils;

/// <summary>
/// Extensions for <see cref="IDistributedApplicationTestingBuilder"/>.
/// </summary>
public static class DistributedApplicationTestingBuilderExtensions
{
    // Returns the unique prefix used for volumes from unnamed volumes this builder
    public static string GetVolumePrefix(this IDistributedApplicationTestingBuilder builder) =>
        $"{VolumeNameGenerator.Sanitize(builder.Environment.ApplicationName).ToLowerInvariant()}-{builder.Configuration["AppHost:Sha256"]!.ToLowerInvariant()[..10]}";

    public static IDistributedApplicationTestingBuilder WithTestAndResourceLogging(this IDistributedApplicationTestingBuilder builder, ITestOutputHelper testOutputHelper)
    {
        builder.Services.AddTestAndResourceLogging(testOutputHelper);
        return builder;
    }

    public static IServiceCollection AddTestAndResourceLogging(this IServiceCollection services, ITestOutputHelper testOutputHelper)
    {
        services.AddXunitLogging(testOutputHelper);
        services.AddLogging(builder =>
        {
            builder.AddFilter("Aspire.Hosting", LogLevel.Trace);
            // Suppress all console logging during tests to reduce noise
            builder.AddFilter<ConsoleLoggerProvider>(null, LogLevel.None);
        });
        return services;
    }

    public static IDistributedApplicationTestingBuilder WithTempAspireStore(this IDistributedApplicationTestingBuilder builder, string? path = null)
    {
        // We create the Aspire Store in a folder with user-only access. This way non-root containers won't be able
        // to access the files unless they correctly assign the required permissions for the container to work.

        builder.Configuration["Aspire:Store:Path"] = path ?? Directory.CreateTempSubdirectory().FullName;
        return builder;
    }

    public static IDistributedApplicationTestingBuilder WithResourceCleanUp(this IDistributedApplicationTestingBuilder builder, bool? resourceCleanup = null)
    {
        builder.Configuration["DcpPublisher:WaitForResourceCleanup"] = resourceCleanup.ToString();
        return builder;
    }

    /// <summary>
    /// Adds xunit logging and suppresses console logging for a host application builder used in tests.
    /// This redirects logs to the xunit test output and prevents console clutter during test runs.
    /// </summary>
    public static IHostApplicationBuilder AddTestLogging(this IHostApplicationBuilder builder, ITestOutputHelper testOutputHelper)
    {
        builder.Logging.AddXunit(testOutputHelper);
        builder.Logging.AddFilter<ConsoleLoggerProvider>(null, LogLevel.None);
        return builder;
    }
}
