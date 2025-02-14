// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.Orchestrator;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Tests.Dcp;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Aspire.Hosting.Utils;

/// <summary>
/// DistributedApplication.CreateBuilder() creates a builder that includes configuration to read from appsettings.json.
/// The builder has a FileSystemWatcher, which can't be cleaned up unless a DistributedApplication is built and disposed.
/// This class wraps the builder and provides a way to automatically dispose it to prevent test failures from excessive
/// FileSystemWatcher instances from many tests.
/// </summary>
public static class TestDistributedApplicationBuilder
{
    public static IDistributedApplicationTestingBuilder Create(DistributedApplicationOperation operation)
    {
        var args = operation switch
        {
            DistributedApplicationOperation.Run => (string[])[],
            DistributedApplicationOperation.Publish => ["Publishing:Publisher=manifest"],
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

        return Create(args);
    }

    public static IDistributedApplicationTestingBuilder Create(params string[] args)
    {
        return CreateCore(args, (_) => { });
    }

    public static IDistributedApplicationTestingBuilder Create(ITestOutputHelper testOutputHelper, params string[] args)
    {
        return CreateCore(args, (_) => { }, testOutputHelper);
    }

    public static IDistributedApplicationTestingBuilder Create(Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        return CreateCore([], configureOptions, testOutputHelper);
    }

    public static IDistributedApplicationTestingBuilder CreateWithTestContainerRegistry(ITestOutputHelper testOutputHelper) =>
        Create(o => o.ContainerRegistryOverride = ComponentTestConstants.AspireTestContainerRegistry, testOutputHelper);

    private static IDistributedApplicationTestingBuilder CreateCore(string[] args, Action<DistributedApplicationOptions>? configureOptions, ITestOutputHelper? testOutputHelper = null)
    {
        var builder = DistributedApplicationTestingBuilder.Create(args, (applicationOptions, hostBuilderOptions) => configureOptions?.Invoke(applicationOptions));

        // TODO: consider centralizing this to DistributedApplicationFactory by default once consumers have a way to opt-out
        // E.g., once https://github.com/dotnet/extensions/pull/5801 is released.
        // Discussion: https://github.com/dotnet/aspire/pull/7335/files#r1936799460
        builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

        builder.Services.AddSingleton<ApplicationOrchestratorProxy>(sp => new ApplicationOrchestratorProxy(sp.GetRequiredService<ApplicationOrchestrator>()));
        if (testOutputHelper is not null)
        {
            builder.WithTestAndResourceLogging(testOutputHelper);
        }

        builder.WithTempAspireStore();

        return builder;
    }
}
