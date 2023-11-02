// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

internal sealed class ExecutableViewModelCache : ViewModelCache<Executable, ExecutableViewModel>
{
    public ExecutableViewModelCache(
        KubernetesService kubernetesService, DistributedApplicationModel applicationModel, ILoggerFactory loggerFactory, CancellationToken cancellationToken)
        : base(kubernetesService, applicationModel, loggerFactory.CreateLogger<ExecutableViewModelCache>(), cancellationToken)
    {
    }

    protected override ExecutableViewModel ConvertToViewModel(
        DistributedApplicationModel applicationModel,
        IEnumerable<Service> services,
        IEnumerable<Endpoint> endpoints,
        Executable executable,
        List<EnvVar>? additionalEnvVars)
    {
        var model = new ExecutableViewModel
        {
            Name = executable.Metadata.Name,
            Uid = executable.Metadata.Uid,
            NamespacedName = new(executable.Metadata.Name, null),
            CreationTimeStamp = executable.Metadata.CreationTimestamp?.ToLocalTime(),
            ExecutablePath = executable.Spec.ExecutablePath,
            WorkingDirectory = executable.Spec.WorkingDirectory,
            Arguments = executable.Spec.Args,
            State = executable.Status?.State,
            LogSource = new FileLogSource(executable.Status?.StdOutFile, executable.Status?.StdErrFile),
            ProcessId = executable.Status?.ProcessId,
            ExpectedEndpointsCount = GetExpectedEndpointsCount(services, executable)
        };

        FillEndpoints(applicationModel, services, endpoints, executable, model);

        if (executable.Status?.EffectiveEnv is not null)
        {
            FillEnvironmentVariables(model.Environment, executable.Status.EffectiveEnv, executable.Spec.Env);
        }
        return model;
    }

    protected override bool FilterResource(Executable resource)
        => resource.Metadata.Annotations is null || !resource.Metadata.Annotations.ContainsKey(Executable.CSharpProjectPathAnnotation);
}
