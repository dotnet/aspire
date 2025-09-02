// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

internal sealed class ResourceDeploymentGraph
{
    private Dictionary<IResource, HashSet<IResource>> Dependencies { get; } = [];
    private HashSet<IResource> Nodes { get; } = [];

    public static ResourceDeploymentGraph Build(IEnumerable<IResource> resources)
    {
        var graph = new ResourceDeploymentGraph();

        foreach (var resource in resources)
        {
            graph.Nodes.Add(resource);
            graph.Dependencies[resource] = [];
        }

        foreach (var resource in resources)
        {
            if (resource is AzureBicepResource bicepResource)
            {
                foreach (var parameter in bicepResource.Parameters.Values)
                {
                    HashSet<IResource> referencedResources;

                    referencedResources = [.. ExtractBicepResourceReferences(parameter)];

                    foreach (var referencedResource in referencedResources)
                    {
                        graph.Dependencies[resource].Add(referencedResource);
                    }
                }
            }
        }

        return graph;
    }

    public async Task ExecuteAsync<TContext>(
        Func<IResource, TContext, Task> executor,
        TContext context,
        CancellationToken cancellationToken = default)
    {
        var stateLock = new object();
        var inProgress = new Dictionary<IResource, Task>();
        var completed = new HashSet<IResource>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var availableNodes = GetAvailableNodes(completed, inProgress.Keys, stateLock);

            if (!availableNodes.Any())
            {
                if (inProgress.Count != 0)
                {
                    var completedTask = await Task.WhenAny(inProgress.Values).ConfigureAwait(false);
                    await HandleTaskCompletion(completedTask, inProgress, completed, stateLock).ConfigureAwait(false);
                    continue;
                }
                break;
            }

            foreach (var node in availableNodes)
            {
                var task = ExecuteNodeAsync(node, context, executor);
                inProgress.TryAdd(node, task);
            }

            if (inProgress.Count != 0)
            {
                var completedTask = await Task.WhenAny(inProgress.Values).ConfigureAwait(false);
                await HandleTaskCompletion(completedTask, inProgress, completed, stateLock).ConfigureAwait(false);
            }
        }
    }

    private IEnumerable<IResource> GetAvailableNodes(
        HashSet<IResource> completed,
        ICollection<IResource> inProgress,
        object stateLock)
    {
        return Nodes.Where(node =>
        {
            lock (stateLock)
            {
                return !completed.Contains(node) &&
                       !inProgress.Contains(node) &&
                       Dependencies[node].All(dep => completed.Contains(dep));
            }
        });
    }

    private static async Task HandleTaskCompletion(
        Task completedTask,
        Dictionary<IResource, Task> inProgress,
        HashSet<IResource> completed,
        object stateLock)
    {
        var completedNode = inProgress.First(kvp => kvp.Value == completedTask).Key;
        inProgress.Remove(completedNode);

        await completedTask.ConfigureAwait(false);

        lock (stateLock)
        {
            completed.Add(completedNode);
        }
    }

    private static async Task ExecuteNodeAsync<TContext>(
        IResource node,
        TContext context,
        Func<IResource, TContext, Task> executor)
    {
        await executor(node, context).ConfigureAwait(false);
    }

    internal static HashSet<AzureBicepResource> ExtractBicepResourceReferences(object? parameterValue)
    {
        var references = new HashSet<AzureBicepResource>();

        switch (parameterValue)
        {
            case BicepOutputReference outputRef:
                references.Add(outputRef.Resource);
                break;

            case IValueWithReferences valueWithRefs:
                foreach (var reference in valueWithRefs.References)
                {
                    references.UnionWith(ExtractBicepResourceReferences(reference));
                }
                break;
        }

        return references;
    }
}
