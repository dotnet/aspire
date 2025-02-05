// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

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
        builder.Services.AddXunitLogging(testOutputHelper);
        builder.Services.AddLogging(builder => builder.AddFilter("Aspire.Hosting", LogLevel.Trace));
        return builder;
    }
}
