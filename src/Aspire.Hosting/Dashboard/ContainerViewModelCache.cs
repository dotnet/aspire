// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal sealed class ContainerViewModelCache : ViewModelCache<Container, ContainerViewModel>
{
    public ContainerViewModelCache(
        KubernetesService kubernetesService, DistributedApplicationModel applicationModel, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        : base(kubernetesService, applicationModel, loggerFactory.CreateLogger<ContainerViewModelCache>(),  cancellationToken)
    {
    }

    protected override ContainerViewModel ConvertToViewModel(
        DistributedApplicationModel applicationModel,
        IEnumerable<Service> services,
        IEnumerable<Endpoint> endpoints,
        Container container,
        List<EnvVar>? additionalEnvVars)
    {
        var model = new ContainerViewModel
        {
            Name = container.Metadata.Name,
            Uid = container.Metadata.Uid,
            NamespacedName = new(container.Metadata.Name, null),
            ContainerId = container.Status?.ContainerId,
            CreationTimeStamp = container.Metadata.CreationTimestamp?.ToLocalTime(),
            Image = container.Spec.Image!,
            LogSource = new DockerContainerLogSource(container.Status!.ContainerId!),
            State = container.Status?.State,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(services, container)
        };

        if (container.Spec.Ports != null)
        {
            foreach (var port in container.Spec.Ports)
            {
                if (port.ContainerPort != null)
                {
                    model.Ports.Add(port.ContainerPort.Value);
                }
            }
        }

        FillEndpoints(applicationModel, services, endpoints, container, model);

        if (additionalEnvVars is not null)
        {
            FillEnvironmentVariables(model.Environment, additionalEnvVars, additionalEnvVars);
        }
        else if (container.Spec.Env is not null)
        {
            FillEnvironmentVariables(model.Environment, container.Spec.Env, container.Spec.Env);
        }

        return model;
    }

    protected override bool FilterResource(Container resource) => true;
}
