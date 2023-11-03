// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp.Model;
using k8s;

namespace Aspire.Hosting.Dcp;

internal class AppResource
{
    public IResource ModelResource { get; private set; }
    public CustomResource DcpResource { get; private set; }
    public virtual List<ServiceAppResource> ServicesProduced { get; private set; } = new();
    public virtual List<ServiceAppResource> ServicesConsumed { get; private set; } = new();

    public AppResource(IResource modelResource, CustomResource dcpResource)
    {
        this.ModelResource = modelResource;
        this.DcpResource = dcpResource;
    }
}

internal sealed class ServiceAppResource : AppResource
{
    public Service Service => (Service)DcpResource;
    public ServiceBindingAnnotation ServiceBindingAnnotation { get; private set; }
    public ServiceProducerAnnotation DcpServiceProducerAnnotation { get; private set; }

    public override List<ServiceAppResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
    public override List<ServiceAppResource> ServicesConsumed
    {
        get { throw new InvalidOperationException("Service resources do not consume any services"); }
    }

    public ServiceAppResource(IResource modelResource, Service service, ServiceBindingAnnotation sba) : base(modelResource, service)
    {
        ServiceBindingAnnotation = sba;
        DcpServiceProducerAnnotation = new(service.Metadata.Name);
    }
}

internal sealed class ApplicationExecutor(DistributedApplicationModel model, KubernetesService kubernetesService)
{
    private const string DebugSessionPortVar = "DEBUG_SESSION_PORT";

    // These environment variables should never be inherited from app host;
    // they only make sense if they come from a launch profile of a service project.
    private static readonly string[] s_doNotInheritEnvironmentVars =
    {
        "ASPNETCORE_URLS",
        "DOTNET_LAUNCH_PROFILE",
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT"
    };

    private readonly DistributedApplicationModel _model = model;
    private readonly List<AppResource> _appResources = new();

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpModelCreationStart();
        try
        {
            PrepareServices();
            PrepareContainers();
            PrepareExecutables();

            await CreateServicesAsync(cancellationToken).ConfigureAwait(false);

            await CreateContainersAndExecutablesAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            AspireEventSource.Instance.DcpModelCreationStop();
        }

    }

    public async Task StopApplicationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AspireEventSource.Instance.DcpModelCleanupStart();
            await DeleteResourcesAsync<ExecutableReplicaSet>("project", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Executable>("project", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Container>("container", cancellationToken).ConfigureAwait(false);
            await DeleteResourcesAsync<Service>("service", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            AspireEventSource.Instance.DcpModelCleanupStop();
            _appResources.Clear();
        }
    }

    private async Task CreateServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AspireEventSource.Instance.DcpServicesCreationStart();

            var needAddressAllocated = _appResources.OfType<ServiceAppResource>().Where(sr => !sr.Service.HasCompleteAddress).ToList();

            await CreateResourcesAsync<Service>(cancellationToken).ConfigureAwait(false);

            if (needAddressAllocated.Count == 0)
            {
                // No need to wait for any updates to Service objects from the orchestrator.
                return;
            }

            // We do not specify the initial list version, so the watcher will give us all updates to Service objects.
            IAsyncEnumerable<(WatchEventType, Service)> serviceChangeEnumerator = kubernetesService.WatchAsync<Service>(cancellationToken: cancellationToken);
            await foreach (var (evt, updated) in serviceChangeEnumerator)
            {
                if (evt == WatchEventType.Bookmark) { continue; } // Bookmarks do not contain any data.

                var srvResource = needAddressAllocated.Where(sr => sr.Service.Metadata.Name == updated.Metadata.Name).FirstOrDefault();
                if (srvResource == null) { continue; } // This service most likely already has full address information, so it is not on needAddressAllocated list.

                if (updated.HasCompleteAddress)
                {
                    srvResource.Service.ApplyAddressInfoFrom(updated);
                    needAddressAllocated.Remove(srvResource);
                }

                if (needAddressAllocated.Count == 0)
                {
                    return; // We are done
                }
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpServicesCreationStop();
        }
    }

    private async Task CreateContainersAndExecutablesAsync(CancellationToken cancellationToken)
    {
        var toCreate = _appResources.Where(r => r.DcpResource is Container || r.DcpResource is Executable || r.DcpResource is ExecutableReplicaSet);
        AddAllocatedEndpointInfo(toCreate);

        await CreateContainersAsync(toCreate.Where(ar => ar.DcpResource is Container), cancellationToken).ConfigureAwait(false);
        await CreateExecutablesAsync(toCreate.Where(ar => ar.DcpResource is Executable || ar.DcpResource is ExecutableReplicaSet), cancellationToken).ConfigureAwait(false);
    }

    private static void AddAllocatedEndpointInfo(IEnumerable<AppResource> resources)
    {
        foreach (var appResource in resources)
        {
            foreach (var sp in appResource.ServicesProduced)
            {
                var svc = (Service)sp.DcpResource;
                if (!svc.HasCompleteAddress)
                {
                    // This should never happen; if it does, we have a bug without a workaround for th the user.
                    throw new InvalidDataException($"Service {svc.Metadata.Name} should have valid address at this point");
                }

                var a = new AllocatedEndpointAnnotation(
                    sp.ServiceBindingAnnotation.Name,
                    PortProtocol.ToProtocolType(svc.Spec.Protocol),
                    svc.AllocatedAddress!,
                    (int)svc.AllocatedPort!,
                    sp.ServiceBindingAnnotation.UriScheme
                    );

                appResource.ModelResource.Annotations.Add(a);
            }
        }
    }

    private void PrepareServices()
    {
        var serviceProducers = _model.Resources
            .Select(r => (ModelResource: r, SBAnnotations: r.Annotations.OfType<ServiceBindingAnnotation>()))
            .Where(sp => sp.SBAnnotations.Any());

        // We need to ensure that Services have unique names (otherwise we cannot really distinguish between
        // services produced by different resources).
        List<string> serviceNames = new();

        void addServiceAppResource(Service svc, IResource producingResource, ServiceBindingAnnotation sba)
        {
            svc.Spec.Protocol = PortProtocol.FromProtocolType(sba.Protocol);
            svc.Spec.AddressAllocationMode = AddressAllocationModes.IPv4Loopback;
            svc.Annotate(CustomResource.UriSchemeAnnotation, sba.UriScheme);

            _appResources.Add(new ServiceAppResource(producingResource, svc, sba));
        }

        foreach (var sp in serviceProducers)
        {
            var sbAnnotations = sp.SBAnnotations.ToArray();
            var replicas = sp.ModelResource.GetReplicaCount();

            foreach (var sba in sbAnnotations)
            {
                var candidateServiceName = sbAnnotations.Length == 1 ?
                    GetObjectNameForResource(sp.ModelResource) : GetObjectNameForResource(sp.ModelResource, sba.Name);
                var uniqueServiceName = GenerateUniqueServiceName(serviceNames, candidateServiceName);
                var svc = Service.Create(uniqueServiceName);

                if (replicas > 1)
                {
                    // Treat the port specified in the ServiceBindingAnnotation as desired port for the whole service.
                    // Each replica receives its own port.
                    svc.Spec.Port = sba.Port;
                }

                addServiceAppResource(svc, sp.ModelResource, sba);
            }
        }
    }

    private void PrepareExecutables()
    {
        PrepareProjectExecutables();
        PreparePlainExecutables();
    }

    private void PreparePlainExecutables()
    {
        var modelExecutableResources = _model.GetExecutableResources();

        foreach (var executable in modelExecutableResources)
        {
            var exeName = GetObjectNameForResource(executable);
            var exePath = executable.Command;
            var exe = Executable.Create(exeName, exePath);

            exe.Spec.WorkingDirectory = executable.WorkingDirectory;
            exe.Spec.Args = executable.Args?.ToList();
            exe.Spec.ExecutionType = ExecutionType.Process;

            var exeAppResource = new AppResource(executable, exe);
            AddServicesProducedInfo(executable, exe, exeAppResource);
            _appResources.Add(exeAppResource);
        }
    }

    private void PrepareProjectExecutables()
    {
        var modelProjectResources = _model.GetProjectResources();

        foreach (var project in modelProjectResources)
        {
            if (!project.TryGetLastAnnotation<IServiceMetadata>(out var projectMetadata))
            {
                throw new InvalidOperationException("A project resource is missing required metadata"); // Should never happen.
            }

            CustomResource workload;
            ExecutableSpec exeSpec;
            IAnnotationHolder annotationHolder;
            var workloadName = GetObjectNameForResource(project);
            int replicas = project.GetReplicaCount();

            if (replicas > 1)
            {
                var ers = ExecutableReplicaSet.Create(workloadName, replicas, "dotnet");
                exeSpec = ers.Spec.Template.Spec;
                annotationHolder = ers.Spec.Template;
                workload = ers;
            }
            else
            {
                var exe = Executable.Create(workloadName, "dotnet");
                exeSpec = exe.Spec;
                annotationHolder = workload = exe;
            }

            exeSpec.WorkingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);

            annotationHolder.Annotate(Executable.CSharpProjectPathAnnotation, projectMetadata.ProjectPath);
            annotationHolder.Annotate(Executable.LaunchProfileNameAnnotation, project.SelectLaunchProfileName() ?? string.Empty);

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DebugSessionPortVar)))
            {
                exeSpec.ExecutionType = ExecutionType.IDE;
            }
            else
            {
                exeSpec.ExecutionType = ExecutionType.Process;
                if (Environment.GetEnvironmentVariable("DOTNET_WATCH") != "1")
                {
                    exeSpec.Args = [
                        "run",
                        "--no-build",
                        "--project",
                        projectMetadata.ProjectPath,
                    ];
                }
                else
                {
                    exeSpec.Args = [
                        "watch",
                        "--non-interactive",
                        "--no-hot-reload",
                        "--project",
                        projectMetadata.ProjectPath
                    ];
                }

                // We pretty much always want to suppress the normal launch profile handling
                // because the settings from the profile will override the ambient environment settings, which is not what we want
                // (the ambient environment settings for service processes come from the application model
                // and should be HIGHER priority than the launch profile settings).
                // This means we need to apply the launch profile settings manually--the invocation parameters here,
                // and the environment variables/application URLs inside CreateExecutableAsync().
                exeSpec.Args.Add("--no-launch-profile");

                string? launchProfileName = project.SelectLaunchProfileName();
                if (!string.IsNullOrEmpty(launchProfileName))
                {
                    var launchProfile = project.GetEffectiveLaunchProfile();
                    if (launchProfile is not null && !string.IsNullOrWhiteSpace(launchProfile.CommandLineArgs))
                    {
                        var cmdArgs = launchProfile.CommandLineArgs.Split((string?)null, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                        if (cmdArgs is not null && cmdArgs.Length > 0)
                        {
                            exeSpec.Args.Add("--");
                            exeSpec.Args.AddRange(cmdArgs);
                        }
                    }
                }
            }

            var exeAppResource = new AppResource(project, workload);
            AddServicesProducedInfo(project, annotationHolder, exeAppResource);
            _appResources.Add(exeAppResource);
        }
    }

    private async Task CreateExecutablesAsync(IEnumerable<AppResource> executableResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpExecutablesCreateStart();

            foreach (var er in executableResources)
            {
                ExecutableSpec spec;
                Func<Task<CustomResource>> createResource;

                switch (er.DcpResource)
                {
                    case Executable exe:
                        spec = exe.Spec;
                        createResource = async () => await kubernetesService.CreateAsync(exe, cancellationToken).ConfigureAwait(false);
                        break;
                    case ExecutableReplicaSet ers:
                        spec = ers.Spec.Template.Spec;
                        createResource = async () => await kubernetesService.CreateAsync(ers, cancellationToken).ConfigureAwait(false);
                        break;
                    default:
                        throw new InvalidOperationException($"Expected an Executable-like resource, but got {er.DcpResource.Kind} instead");
                }

                spec.Args ??= new();

                if (er.ModelResource.TryGetAnnotationsOfType<ExecutableArgsCallbackAnnotation>(out var exeArgsCallbacks))
                {
                    foreach (var exeArgsCallback in exeArgsCallbacks)
                    {
                        exeArgsCallback.Callback(spec.Args);
                    }
                }

                var config = new Dictionary<string, string>();
                var context = new EnvironmentCallbackContext("dcp", config);

                // Need to apply configuration settings manually; see PrepareExecutables() for details.
                if (er.ModelResource is ProjectResource project && project.SelectLaunchProfileName() is { } launchProfileName && project.GetLaunchSettings() is { } launchSettings)
                {
                    ApplyLaunchProfile(er, config, launchProfileName, launchSettings);
                }
                else
                {
                    // If there is no launch profile, we want to make sure that certain environment variables are NOT inherited
                    foreach (var envVar in s_doNotInheritEnvironmentVars)
                    {
                        config.Add(envVar, "");
                    }
                }

                if (er.ModelResource.TryGetEnvironmentVariables(out var envVarAnnotations))
                {
                    foreach (var ann in envVarAnnotations)
                    {
                        ann.Callback(context);
                    }
                }

                spec.Env = new();
                foreach (var c in config)
                {
                    spec.Env.Add(new EnvVar { Name = c.Key, Value = c.Value });
                }

                await createResource().ConfigureAwait(false);
            }

        }
        finally
        {
            AspireEventSource.Instance.DcpExecutablesCreateStop();
        }
    }

    private static void ApplyLaunchProfile(AppResource executableResource, Dictionary<string, string> config, string launchProfileName, LaunchSettings launchSettings)
    {
        // Populate DOTNET_LAUNCH_PROFILE environment variable for consistency with "dotnet run" and "dotnet watch".
        config.Add("DOTNET_LAUNCH_PROFILE", launchProfileName);

        var launchProfile = launchSettings.Profiles[launchProfileName];
        if (!string.IsNullOrWhiteSpace(launchProfile.ApplicationUrl))
        {
            int replicas = executableResource.ModelResource.GetReplicaCount();

            if (replicas > 1)
            {
                // Can't use the information in ASPNETCORE_URLS directly when multiple replicas are in play.
                // Instead we are going to SYNTHESIZE the new ASPNETCORE_URLS value based on the information about services produced by this resource.
                var urls = executableResource.ServicesProduced.Select(sar =>
                {
                    var url = sar.ServiceBindingAnnotation.UriScheme + "://localhost:{{- portForServing \"" + sar.Service.Metadata.Name + "\" -}}";
                    return url;
                });
                config.Add("ASPNETCORE_URLS", string.Join(";", urls));
            }
            else
            {
                config.Add("ASPNETCORE_URLS", launchProfile.ApplicationUrl);
            }
        }

        foreach (var envVar in launchProfile.EnvironmentVariables)
        {
            string value = Environment.ExpandEnvironmentVariables(envVar.Value);
            config[envVar.Key] = value;
        }
    }

    private void PrepareContainers()
    {
        var modelContainerResources = _model.GetContainerResources();

        foreach (var container in modelContainerResources)
        {
            if (!container.TryGetContainerImageName(out var containerImageName))
            {
                // This should never happen! In order to get into this loop we need
                // to have the annotation, if we don't have the annotation by the time
                // we get here someone is doing something wrong.
                throw new InvalidOperationException();
            }

            var ctr = Container.Create(container.Name, containerImageName);

            if (container.TryGetVolumeMounts(out var volumeMounts))
            {
                ctr.Spec.VolumeMounts = new();

                foreach (var mount in volumeMounts)
                {
                    bool isBound = mount.Type == ApplicationModel.VolumeMountType.Bind;
                    var volumeSpec = new VolumeMount()
                    {
                        Source = isBound ? Path.GetFullPath(mount.Source) : mount.Source,
                        Target = mount.Target,
                        Type = isBound ? Model.VolumeMountType.Bind : Model.VolumeMountType.Named,
                        IsReadOnly = mount.IsReadOnly
                    };
                    ctr.Spec.VolumeMounts.Add(volumeSpec);
                }
            }

            var containerAppResource = new AppResource(container, ctr);
            AddServicesProducedInfo(container, ctr, containerAppResource);
            _appResources.Add(containerAppResource);
        }
    }

    private async Task CreateContainersAsync(IEnumerable<AppResource> containerResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpContainersCreateStart();

            foreach (var cr in containerResources)
            {
                var dcpContainerResource = (Container)cr.DcpResource;
                var modelContainerResource = cr.ModelResource;

                dcpContainerResource.Spec.Env = new();

                if (modelContainerResource.TryGetEnvironmentVariables(out var containerEnvironmentVariables))
                {
                    var config = new Dictionary<string, string>();
                    var context = new EnvironmentCallbackContext("dcp", config);

                    foreach (var v in containerEnvironmentVariables)
                    {
                        v.Callback(context);
                    }

                    foreach (var kvp in config)
                    {
                        dcpContainerResource.Spec.Env.Add(new EnvVar { Name = kvp.Key, Value = kvp.Value });
                    }
                }

                if (cr.ServicesProduced.Count > 0)
                {
                    dcpContainerResource.Spec.Ports = new();

                    foreach (var sp in cr.ServicesProduced)
                    {
                        var portSpec = new ContainerPortSpec()
                        {
                            ContainerPort = sp.DcpServiceProducerAnnotation.Port,
                        };

                        if (!string.IsNullOrEmpty(sp.DcpServiceProducerAnnotation.Address))
                        {
                            portSpec.HostIP = sp.DcpServiceProducerAnnotation.Address;
                        }

                        if (sp.ServiceBindingAnnotation.Port is not null)
                        {
                            portSpec.HostPort = sp.ServiceBindingAnnotation.Port;
                        }

                        switch (sp.ServiceBindingAnnotation.Protocol)
                        {
                            case ProtocolType.Tcp:
                                portSpec.Protocol = PortProtocol.TCP; break;
                            case ProtocolType.Udp:
                                portSpec.Protocol = PortProtocol.UDP; break;
                        }

                        dcpContainerResource.Spec.Ports.Add(portSpec);
                    }
                }

                await kubernetesService.CreateAsync(dcpContainerResource, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            AspireEventSource.Instance.DcpContainersCreateStop();
        }
    }

    private void AddServicesProducedInfo(IResource modelResource, IAnnotationHolder dcpResource, AppResource appResource)
    {
        string modelResourceName = "(unknown)";
        try
        {
            modelResourceName = GetObjectNameForResource(modelResource);
        }
        catch { } // For error messages only, OK to fall back to (unknown)

        var servicesProduced = _appResources.OfType<ServiceAppResource>().Where(r => r.ModelResource == modelResource);
        foreach (var sp in servicesProduced)
        {
            if (modelResource.IsContainer())
            {
                if (sp.ServiceBindingAnnotation.ContainerPort is null)
                {
                    throw new InvalidOperationException($"The ServiceBindingAnnotation for container resource {modelResourceName} must specify the ContainerPort");
                }

                sp.DcpServiceProducerAnnotation.Port = sp.ServiceBindingAnnotation.ContainerPort;
            }
            else if (modelResource is ExecutableResource)
            {
                sp.DcpServiceProducerAnnotation.Port = sp.ServiceBindingAnnotation.Port;
            }
            else
            {
                if (sp.ServiceBindingAnnotation.Port is null)
                {
                    throw new InvalidOperationException($"The ServiceBindingAnnotation for resource {modelResourceName} must specify the Port");
                }

                if (modelResource.GetReplicaCount() == 1)
                {
                    // If multiple replicas are used, each replica will get its own port.
                    sp.DcpServiceProducerAnnotation.Port = sp.ServiceBindingAnnotation.Port;
                }
            }

            dcpResource.AnnotateAsObjectList(CustomResource.ServiceProducerAnnotation, sp.DcpServiceProducerAnnotation);
            appResource.ServicesProduced.Add(sp);
        }
    }

    private async Task CreateResourcesAsync<RT>(CancellationToken cancellationToken) where RT : CustomResource
    {
        var resourcesToCreate = _appResources.Select(r => r.DcpResource).OfType<RT>();
        if (!resourcesToCreate.Any())
        {
            return;
        }

        // CONSIDER batched creation
        foreach (var res in resourcesToCreate)
        {
            await kubernetesService.CreateAsync(res, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DeleteResourcesAsync<RT>(string resourceName, CancellationToken cancellationToken) where RT : CustomResource
    {
        var resourcesToDelete = _appResources.Select(r => r.DcpResource).OfType<RT>();
        if (!resourcesToDelete.Any())
        {
            return;
        }

        foreach (var res in resourcesToDelete)
        {
            try
            {
                await kubernetesService.DeleteAsync<RT>(res.Metadata.Name, res.Metadata.NamespaceProperty, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not stop {resourceName} '{res.Metadata.Name}': {ex}");
            }
        }
    }

    private static string GetObjectNameForResource(IResource resource, string suffix = "")
    {
        string maybeWithSuffix(string s) => string.IsNullOrWhiteSpace(suffix) ? s : $"{s}_{suffix}";
        return maybeWithSuffix(resource.Name);
    }

    private static string GenerateUniqueServiceName(List<string> serviceNames, string candidateName)
    {
        int suffix = 1;
        string uniqueName = candidateName;

        while (serviceNames.Contains(uniqueName))
        {
            uniqueName = $"{candidateName}_{suffix}";
            suffix++;
            if (suffix == 100)
            {
                // Should never happen, but we do not want to ever get into a infinite loop situation either.
                throw new ArgumentException($"Could not generate a unique name for service '{candidateName}'");
            }
        }

        serviceNames.Add(uniqueName);
        return uniqueName;
    }

}
