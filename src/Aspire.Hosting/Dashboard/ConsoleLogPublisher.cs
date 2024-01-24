// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

internal sealed class ConsoleLogPublisher(ResourcePublisher resourcePublisher)
{
    internal IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? Subscribe(string resourceName)
    {
        // Look up the requested resource, so we know how to obtain logs.
        if (!resourcePublisher.TryGetResource(resourceName, out var resource))
        {
            throw new ArgumentException($"Unknown resource {resourceName}.", nameof(resourceName));
        }

        // Obtain logs using the relevant approach.
        // Note, we would like to obtain these logs via DCP directly, rather than sourcing them in the dashboard.
        return resource switch
        {
            ExecutableSnapshot executable => SubscribeExecutable(executable),
            ContainerSnapshot container => SubscribeContainer(container),
            _ => throw new NotSupportedException($"Unsupported resource type {resource.GetType()}.")
        };

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
