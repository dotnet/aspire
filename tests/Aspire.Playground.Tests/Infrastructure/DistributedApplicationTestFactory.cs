// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SamplesIntegrationTests.Infrastructure;

namespace SamplesIntegrationTests;

internal static class DistributedApplicationTestFactory
{
    /// <summary>
    /// Creates an <see cref="IDistributedApplicationTestingBuilder"/> for the specified app host assembly.
    /// </summary>
    public static async Task<IDistributedApplicationTestingBuilder> CreateAsync(Type appHostProgramType, ITestOutputHelper? testOutput)
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync(appHostProgramType);

        // Custom hook needed because we want to only override the registry when
        // the original is from `docker.io`, but the options.ContainerRegistryOverride will
        // always override.
        builder.Services.AddLifecycleHook<ContainerRegistryHook>();

        builder.WithRandomParameterValues();
        builder.WithRandomVolumeNames();

        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(configure =>
            {
                configure.SingleLine = true;
            });
            logging.AddFakeLogging();
            if (testOutput is not null)
            {
                logging.AddXunit(testOutput);
            }
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddFilter("Aspire", LogLevel.Trace);
            logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Trace);
        });

        return builder;
    }

    internal sealed class ContainerRegistryHook : IDistributedApplicationLifecycleHook
    {
        public const string AspireTestContainerRegistry = "netaspireci.azurecr.io";
        public Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
        {
            var resourcesWithContainerImages = appModel.Resources
                                                       .SelectMany(r => r.Annotations.OfType<ContainerImageAnnotation>()
                                                                                     .Select(cia => new { Resource = r, Annotation = cia }));

            foreach (var resourceWithContainerImage in resourcesWithContainerImages)
            {
                string? registry = resourceWithContainerImage.Annotation.Registry;
                if (registry is null || registry.Contains("docker.io"))
                {
                    resourceWithContainerImage.Annotation.Registry = AspireTestContainerRegistry;
                }
            }

            return Task.CompletedTask;
        }
    }

}
