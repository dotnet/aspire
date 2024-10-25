// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;

namespace Aspire.Hosting;

/// <summary>
/// TODO
/// </summary>
public class ContainerCommandExecutor
{
    private readonly IKubernetesService _k8s;

    internal ContainerCommandExecutor(IKubernetesService k8s)
    {
        _k8s = k8s;
    }

    /// <summary>
    /// TODO:
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="command"></param>
    /// <param name="args"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task ExecuteAsync(ContainerResource resource, string command, string[] args, CancellationToken cancellationToken = default)
    {
        if (!resource.TryGetLastAnnotation<DcpInstancesAnnotation>(out var annotation))
        {
            return;
        }

        var spec = new ContainerExecSpec()
        {
            ContainerName = annotation.Instances.First().Name,
            Command = command,
            Args = args.ToList()
        };

        var exec = new ContainerExec(spec);
        exec.Metadata.Name = "ls";
        await _k8s.CreateAsync(exec, cancellationToken).ConfigureAwait(false);
    }
}
