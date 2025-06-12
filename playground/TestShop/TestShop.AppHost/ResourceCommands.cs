// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

internal static class ResourceCommands
{
    internal static async Task ExecuteCommandForAllResourcesAsync(IServiceProvider serviceProvider, string commandName, CancellationToken cancellationToken)
    {
        var commandService = serviceProvider.GetRequiredService<ResourceCommandService>();
        var model = serviceProvider.GetRequiredService<DistributedApplicationModel>();

        var resources = model.Resources
            .Where(r => r.IsContainer() || r is ProjectResource || r is ExecutableResource)
            .Where(r => r.Name != KnownResourceNames.AspireDashboard)
            .ToList();

        var commandTasks = new List<Task>();
        foreach (var r in resources)
        {
            commandTasks.Add(commandService.ExecuteCommandAsync(r, commandName, cancellationToken));
        }
        await Task.WhenAll(commandTasks).ConfigureAwait(false);
    }
}
