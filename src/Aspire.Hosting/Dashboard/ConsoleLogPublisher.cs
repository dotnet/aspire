// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

internal sealed class ConsoleLogPublisher
{
    private readonly ResourcePublisher _resourcePublisher;
    private readonly CancellationToken _cancellationToken;

    public ConsoleLogPublisher(ResourcePublisher resourcePublisher, CancellationToken cancellationToken)
    {
        _resourcePublisher = resourcePublisher;
        _cancellationToken = cancellationToken;
    }

    internal IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? Subscribe(string resourceName)
    {
        // Look up the requested resource, so we know how to obtain logs.
        if (!_resourcePublisher.TryGetResource(resourceName, out var resource))
        {
            throw new ArgumentException($"Unknown resource {resourceName}.", nameof(resourceName));
        }

        // Obtain logs using the relevant approach.
        // Note, we would like to obtain these logs via DCP directly, rather than sourcing them in the dashboard.
        return resource switch
        {
            ExecutableSnapshot executable => SubscribeExecutable(executable, _cancellationToken),
            ContainerSnapshot container => SubscribeContainer(container, _cancellationToken),
            _ => throw new NotSupportedException($"Unsupported resource type {resource.GetType()}.")
        };

        static FileLogSource? SubscribeExecutable(ExecutableSnapshot executable, CancellationToken cancellationToken)
        {
            if (executable.StdOutFile is null || executable.StdErrFile is null)
            {
                return null;
            }

            return new FileLogSource(executable.StdOutFile, executable.StdErrFile, cancellationToken);
        }

        static DockerContainerLogSource? SubscribeContainer(ContainerSnapshot container, CancellationToken cancellationToken)
        {
            if (container.ContainerId is null)
            {
                return null;
            }

            return new DockerContainerLogSource(container.ContainerId, cancellationToken);
        }
    }
}
