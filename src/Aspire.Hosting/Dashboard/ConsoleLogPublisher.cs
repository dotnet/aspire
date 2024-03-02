// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

using LogsEnumerable = IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>;

internal sealed class ConsoleLogPublisher(
    ResourcePublisher resourcePublisher,
    ResourceLoggerService resourceLoggerService,
    IKubernetesService kubernetesService,
    ILoggerFactory loggerFactory,
    IConfiguration configuration)
{
    internal LogsEnumerable? Subscribe(string resourceName)
    {
        // Look up the requested resource, so we know how to obtain logs.
        if (!resourcePublisher.TryGetResource(resourceName, out var resource))
        {
            throw new ArgumentException($"Unknown resource {resourceName}.", nameof(resourceName));
        }

        // Obtain logs using the relevant approach.
        if (configuration.GetBool("DOTNET_ASPIRE_USE_STREAMING_LOGS") is true)
        {
            return resource switch
            {
                ExecutableSnapshot executable => SubscribeExecutableResource(executable),
                ContainerSnapshot container => SubscribeContainerResource(container),
                GenericResourceSnapshot genericResource => resourceLoggerService.WatchAsync(genericResource.Name),
                _ => throw new NotSupportedException($"Unsupported resource type {resource.GetType()}.")
            };
        }
        else
        {
            return resource switch
            {
                ExecutableSnapshot executable => SubscribeExecutable(executable),
                ContainerSnapshot container => SubscribeContainer(container),
                GenericResourceSnapshot genericResource => resourceLoggerService.WatchAsync(genericResource.Name),
                _ => throw new NotSupportedException($"Unsupported resource type {resource.GetType()}.")
            };
        }

        LogsEnumerable SubscribeExecutableResource(ExecutableSnapshot executable)
        {
            var executableIdentity = Executable.Create(executable.Name, string.Empty);
            return new ResourceLogSource<Executable>(loggerFactory, kubernetesService, executableIdentity);
        }

        LogsEnumerable SubscribeContainerResource(ContainerSnapshot container)
        {
            var containerIdentity = Container.Create(container.Name, string.Empty);
            return new ResourceLogSource<Container>(loggerFactory, kubernetesService, containerIdentity);
        }

        static FileLogSource? SubscribeExecutable(ExecutableSnapshot executable)
        {
            if (executable.StdOutFile is null || executable.StdErrFile is null)
            {
                return null;
            }

            return new FileLogSource(executable.StdOutFile, executable.StdErrFile);
        }

        static DockerContainerLogSource? SubscribeContainer(ContainerSnapshot container)
        {
            if (container.ContainerId is null)
            {
                return null;
            }

            return new DockerContainerLogSource(container.ContainerId);
        }
    }
}
