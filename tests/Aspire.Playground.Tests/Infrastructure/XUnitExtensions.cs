// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SamplesIntegrationTests.Infrastructure;
using Xunit.Abstractions;

namespace SamplesIntegrationTests;

internal static partial class DistributedApplicationTestFactory
{
    /// <summary>
    /// Creates an <see cref="IDistributedApplicationTestingBuilder"/> for the specified app host assembly and outputs logs to the provided <see cref="ITestOutputHelper"/>.
    /// </summary>
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(string appHostAssemblyPath, ITestOutputHelper testOutputHelper)
    {
        var builder = await CreateAsync(appHostAssemblyPath, new XUnitTextWriter(testOutputHelper));
        builder.Services.AddSingleton<ILoggerProvider, XUnitLoggerProvider>();
        builder.Services.AddSingleton(testOutputHelper);
        return builder;
    }
}
