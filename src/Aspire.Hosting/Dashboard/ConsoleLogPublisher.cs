// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dashboard;

internal sealed class ConsoleLogPublisher : IDisposable
{
    private readonly ResourcePublisher _resourcePublisher;
    private readonly CancellationTokenSource _cts;

    public ConsoleLogPublisher(ResourcePublisher resourcePublisher)
    {
        _resourcePublisher = resourcePublisher;
        _cts = new();
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
            ExecutableSnapshot executable => SubscribeExecutable(executable, _cts.Token),
            ContainerSnapshot container => SubscribeContainer(container, _cts.Token),
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

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
