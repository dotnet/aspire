// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Lifecycle;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Dcp;

internal class AppResource
{
    public IResource ModelResource { get; }
    public CustomResource DcpResource { get; }
    public virtual List<ServiceAppResource> ServicesProduced { get; } = [];
    public virtual List<ServiceAppResource> ServicesConsumed { get; } = [];

    public AppResource(IResource modelResource, CustomResource dcpResource)
    {
        ModelResource = modelResource;
        DcpResource = dcpResource;
    }
}

internal sealed class ServiceAppResource : AppResource
{
    public Service Service => (Service)DcpResource;
    public EndpointAnnotation EndpointAnnotation { get; }
    public ServiceProducerAnnotation DcpServiceProducerAnnotation { get; }

    public override List<ServiceAppResource> ServicesProduced
    {
        get { throw new InvalidOperationException("Service resources do not produce any services"); }
    }
    public override List<ServiceAppResource> ServicesConsumed
    {
        get { throw new InvalidOperationException("Service resources do not consume any services"); }
    }

    public ServiceAppResource(IResource modelResource, Service service, EndpointAnnotation sba) : base(modelResource, service)
    {
        EndpointAnnotation = sba;
        DcpServiceProducerAnnotation = new(service.Metadata.Name);
    }
}

internal sealed class ApplicationExecutor(ILogger<ApplicationExecutor> logger,
                                          DistributedApplicationModel model,
                                          DistributedApplicationOptions distributedApplicationOptions,
                                          KubernetesService kubernetesService,
                                          IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks,
                                          IEnvironmentVariables environmentVariables,
                                          IOptions<DcpOptions> options,
                                          DashboardServiceHost dashboardHost)
{
    private const string DebugSessionPortVar = "DEBUG_SESSION_PORT";

    private readonly ILogger<ApplicationExecutor> _logger = logger;
    private readonly DistributedApplicationModel _model = model;
    private readonly IDistributedApplicationLifecycleHook[] _lifecycleHooks = lifecycleHooks.ToArray();
    private readonly IOptions<DcpOptions> _options = options;
    private readonly DashboardServiceHost _dashboardServiceHost = dashboardHost;
    private readonly List<AppResource> _appResources = [];

    // These environment variables should never be inherited from app host;
    // they only make sense if they come from a launch profile of a service project.
    private static readonly string[] s_doNotInheritEnvironmentVars =
    {
        "ASPNETCORE_URLS",
        "DOTNET_LAUNCH_PROFILE",
        "ASPNETCORE_ENVIRONMENT",
        "DOTNET_ENVIRONMENT"
    };

    public async Task RunApplicationAsync(CancellationToken cancellationToken = default)
    {
        AspireEventSource.Instance.DcpModelCreationStart();
        try
        {
            if (!distributedApplicationOptions.DisableDashboard)
            {
                if (_model.Resources.SingleOrDefault(r => StringComparers.ResourceName.Equals(r.Name, KnownResourceNames.AspireDashboard)) is not { } dashboardResource)
                {
                    // No dashboard is specified, so start one.
                    // TODO validate that the dashboard has not been suppressed
                    await StartDashboardAsDcpExecutableAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ConfigureAspireDashboardResource(dashboardResource, cancellationToken).ConfigureAwait(false);
                }
            }

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

    private async Task ConfigureAspireDashboardResource(IResource dashboardResource, CancellationToken cancellationToken)
    {
        // Don't publish the resource to the manifest.
        dashboardResource.Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        // Remove endpoint annotations because we are directly configuring
        // the dashboard app (it doesn't go through the proxy!).
        var endpointAnnotations = dashboardResource.Annotations.OfType<EndpointAnnotation>().ToList();
        foreach (var endpointAnnotation in endpointAnnotations)
        {
            dashboardResource.Annotations.Remove(endpointAnnotation);
        }

        // Get resource endpoint URL.
        string grpcEndpointUrl;
        try
        {
            grpcEndpointUrl = await _dashboardServiceHost.GetResourceServiceUriAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DistributedApplicationException("Error getting the resource service URL.", ex);
        }

        dashboardResource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is not { } appHostApplicationUrl)
            {
                throw new DistributedApplicationException("Dashboard inner loop hook failed to configure resource because ASPNETCORE_URLS environment variable was not set.");
            }

            if (Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL") is not { } otlpEndpointUrl)
            {
                throw new DistributedApplicationException("Dashboard inner loop hook failed to configure resource because DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable was not set.");
            }

            // Grab the resource service URL. We need to inject this into the resource.

            context.EnvironmentVariables["ASPNETCORE_URLS"] = appHostApplicationUrl;
            context.EnvironmentVariables["DOTNET_RESOURCE_SERVICE_ENDPOINT_URL"] = grpcEndpointUrl;
            context.EnvironmentVariables["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = otlpEndpointUrl;
        }));
    }

    private async Task StartDashboardAsDcpExecutableAsync(CancellationToken cancellationToken = default)
    {
        if (!distributedApplicationOptions.DashboardEnabled)
        {
            // The dashboard is disabled. Do nothing.
            return;
        }

        if (_options.Value.DashboardPath is not { } dashboardPath)
        {
            throw new DistributedApplicationException("Dashboard path empty or file does not exist.");
        }

        var fullyQualifiedDashboardPath = Path.GetFullPath(dashboardPath);
        var dashboardWorkingDirectory = Path.GetDirectoryName(fullyQualifiedDashboardPath);

        var dashboardExecutableSpec = new ExecutableSpec
        {
            ExecutionType = ExecutionType.Process,
            WorkingDirectory = dashboardWorkingDirectory
        };

        if (string.Equals(".dll", Path.GetExtension(fullyQualifiedDashboardPath), StringComparison.OrdinalIgnoreCase))
        {
            // The dashboard path is a DLL, so run it with `dotnet <dll>`
            dashboardExecutableSpec.ExecutablePath = "dotnet";
            dashboardExecutableSpec.Args = [fullyQualifiedDashboardPath];
        }
        else
        {
            // Assume the dashboard path is directly executable
            dashboardExecutableSpec.ExecutablePath = fullyQualifiedDashboardPath;
        }

        string grpcEndpointUrl;
        try
        {
            grpcEndpointUrl = await _dashboardServiceHost.GetResourceServiceUriAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new DistributedApplicationException("Error getting the resource service URL.", ex);
        }

        var otlpEndpointUrl = environmentVariables.GetString("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL");
        var dashboardUrl = environmentVariables.GetString("ASPNETCORE_URLS") ?? throw new DistributedApplicationException("ASPNETCORE_URLS environment variable not set.");
        var aspnetcoreEnvironment = environmentVariables.GetString("ASPNETCORE_ENVIRONMENT");

        dashboardExecutableSpec.Env =
        [
            new()
            {
                Name = "DOTNET_RESOURCE_SERVICE_ENDPOINT_URL",
                Value = grpcEndpointUrl
            },
            new()
            {
                Name = "ASPNETCORE_URLS",
                Value = dashboardUrl
            },
            new()
            {
                Name = "DOTNET_DASHBOARD_OTLP_ENDPOINT_URL",
                Value = otlpEndpointUrl
            },
            new()
            {
                Name = "ASPNETCORE_ENVIRONMENT",
                Value = aspnetcoreEnvironment
            }
        ];

        var dashboardExecutable = new Executable(dashboardExecutableSpec)
        {
            Metadata = { Name = KnownResourceNames.AspireDashboard }
        };

        await kubernetesService.CreateAsync(dashboardExecutable, cancellationToken).ConfigureAwait(false);
        await WaitForHttpSuccessOrThrow(dashboardUrl, DashboardAvailabilityTimeoutDuration, cancellationToken).ConfigureAwait(false);
    }

    private static TimeSpan DashboardAvailabilityTimeoutDuration
    {
        get
        {
            if (Environment.GetEnvironmentVariable("DOTNET_ASPIRE_DASHBOARD_TIMEOUT_SECONDS") is { } timeoutString && int.TryParse(timeoutString, out var timeoutInSeconds))
            {
                return TimeSpan.FromSeconds(timeoutInSeconds);
            }
            else
            {
                return TimeSpan.FromSeconds(DefaultDashboardAvailabilityTimeoutDurationInSeconds);
            }
        }
    }

    private const int DefaultDashboardAvailabilityTimeoutDurationInSeconds = 60;

    private async Task WaitForHttpSuccessOrThrow(string url, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var client = new HttpClient();

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Dashboard not ready yet.");
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), linkedCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Only display this error if the timeout CTS was the one that was cancelled.
            throw new DistributedApplicationException($"Timed out after {timeout} while waiting for the dashboard to be responsive.");
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
            await foreach (var (evt, updated) in serviceChangeEnumerator.ConfigureAwait(true)) // Setting ConfigureAwait to silence analyzer. Consider calling ConfigureAwait(false)
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

        foreach (var lifecycleHook in _lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(_model, cancellationToken).ConfigureAwait(false);
        }

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
                    sp.EndpointAnnotation.Name,
                    PortProtocol.ToProtocolType(svc.Spec.Protocol),
                    svc.AllocatedAddress!,
                    (int)svc.AllocatedPort!,
                    sp.EndpointAnnotation.UriScheme
                    );

                appResource.ModelResource.Annotations.Add(a);
            }
        }
    }

    private void PrepareServices()
    {
        var serviceProducers = _model.Resources
            .Select(r => (ModelResource: r, SBAnnotations: r.Annotations.OfType<EndpointAnnotation>()))
            .Where(sp => sp.SBAnnotations.Any());

        // We need to ensure that Services have unique names (otherwise we cannot really distinguish between
        // services produced by different resources).
        List<string> serviceNames = new();

        void addServiceAppResource(Service svc, IResource producingResource, EndpointAnnotation sba)
        {
            svc.Spec.Protocol = PortProtocol.FromProtocolType(sba.Protocol);
            svc.Annotate(CustomResource.UriSchemeAnnotation, sba.UriScheme);
            svc.Spec.AddressAllocationMode = AddressAllocationModes.Localhost;
            _appResources.Add(new ServiceAppResource(producingResource, svc, sba));
        }

        foreach (var sp in serviceProducers)
        {
            var sbAnnotations = sp.SBAnnotations.ToArray();

            foreach (var sba in sbAnnotations)
            {
                var candidateServiceName = sbAnnotations.Length == 1 ?
                    GetObjectNameForResource(sp.ModelResource) : GetObjectNameForResource(sp.ModelResource, sba.Name);
                var uniqueServiceName = GenerateUniqueServiceName(serviceNames, candidateServiceName);
                var svc = Service.Create(uniqueServiceName);

                svc.Spec.Port = sba.Port;

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

            // The working directory is always relative to the app host project directory (if it exists).
            exe.Spec.WorkingDirectory = executable.WorkingDirectory;
            exe.Spec.Args = executable.Args?.ToList();
            exe.Spec.ExecutionType = ExecutionType.Process;
            exe.Annotate(Executable.OtelServiceNameAnnotation, exe.Metadata.Name);

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
            if (!project.TryGetLastAnnotation<IProjectMetadata>(out var projectMetadata))
            {
                throw new InvalidOperationException("A project resource is missing required metadata"); // Should never happen.
            }

            int replicas = project.GetReplicaCount();

            var ers = ExecutableReplicaSet.Create(GetObjectNameForResource(project), replicas, "dotnet");
            var exeSpec = ers.Spec.Template.Spec;
            IAnnotationHolder annotationHolder = ers.Spec.Template;

            exeSpec.WorkingDirectory = Path.GetDirectoryName(projectMetadata.ProjectPath);

            annotationHolder.Annotate(Executable.CSharpProjectPathAnnotation, projectMetadata.ProjectPath);
            annotationHolder.Annotate(Executable.OtelServiceNameAnnotation, ers.Metadata.Name);

            if (!string.IsNullOrEmpty(environmentVariables.GetString(DebugSessionPortVar)))
            {
                exeSpec.ExecutionType = ExecutionType.IDE;
                if (project.TryGetLastAnnotation<LaunchProfileAnnotation>(out var lpa))
                {
                    annotationHolder.Annotate(Executable.CSharpLaunchProfileAnnotation, lpa.LaunchProfileName);
                }
            }
            else
            {
                exeSpec.ExecutionType = ExecutionType.Process;
                if (environmentVariables.GetBool("DOTNET_WATCH") is true)
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

            var exeAppResource = new AppResource(project, ers);
            AddServicesProducedInfo(project, annotationHolder, exeAppResource);
            _appResources.Add(exeAppResource);
        }
    }

    private async Task CreateExecutablesAsync(IEnumerable<AppResource> executableResources, CancellationToken cancellationToken)
    {
        try
        {
            AspireEventSource.Instance.DcpExecutablesCreateStart();

            // Hoisting the aspire-dashboard resource if it exists to the top of
            // the list so we start it first.
            var sortedExecutableResources = executableResources.ToList();
            var (dashboardIndex, dashboardAppResource) = sortedExecutableResources.IndexOf(static r => StringComparers.ResourceName.Equals(r.ModelResource.Name, KnownResourceNames.AspireDashboard));

            if (dashboardIndex > 0)
            {
                sortedExecutableResources.RemoveAt(dashboardIndex);
                sortedExecutableResources.Insert(0, dashboardAppResource);
            }

            foreach (var er in sortedExecutableResources)
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

                    if (er.ServicesProduced.Count > 0)
                    {
                        if (er.ModelResource is ProjectResource)
                        {
                            var urls = er.ServicesProduced.Where(s => s.EndpointAnnotation.UriScheme is "http" or "https").Select(sar =>
                            {
                                var url = sar.EndpointAnnotation.UriScheme + "://localhost:{{- portForServing \"" + sar.Service.Metadata.Name + "\" -}}";
                                return url;
                            });

                            // REVIEW: Should we assume ASP.NET Core?
                            // We're going to use http and https urls as ASPNETCORE_URLS
                            config["ASPNETCORE_URLS"] = string.Join(";", urls);
                        }

                        InjectPortEnvVars(er, config);
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

                // NOTE: This check is only necessary for the inner loop in the dotnet/aspire repo. When
                //       running in the dotnet/aspire repo we will normally launch the dashboard via
                //       AddProject<T>. When doing this we make sure that the dashboard is running.
                if (!distributedApplicationOptions.DisableDashboard && er.ModelResource.Name.Equals(KnownResourceNames.AspireDashboard, StringComparisons.ResourceName))
                {
                    // We just check the HTTP endpoint because this will prove that the
                    // dashboard is listening and is ready to process requests.
                    if (Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is not { } dashboardUrl)
                    {
                        throw new DistributedApplicationException("Cannot check dashboard availability since ASPNETCORE_URLS environment variable not set.");
                    }

                    await WaitForHttpSuccessOrThrow(dashboardUrl, DashboardAvailabilityTimeoutDuration, cancellationToken).ConfigureAwait(false);
                }

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
            if (executableResource.DcpResource is ExecutableReplicaSet)
            {
                var urls = executableResource.ServicesProduced.Select(sar =>
                {
                    var url = sar.EndpointAnnotation.UriScheme + "://localhost:{{- portForServing \"" + sar.Service.Metadata.Name + "\" -}}";
                    return url;
                });

                config.Add("ASPNETCORE_URLS", string.Join(";", urls));
            }
            else
            {
                config.Add("ASPNETCORE_URLS", launchProfile.ApplicationUrl);
            }

            InjectPortEnvVars(executableResource, config);
        }

        foreach (var envVar in launchProfile.EnvironmentVariables)
        {
            string value = Environment.ExpandEnvironmentVariables(envVar.Value);
            config[envVar.Key] = value;
        }
    }

    private static void InjectPortEnvVars(AppResource executableResource, Dictionary<string, string> config)
    {
        ServiceAppResource? httpsServiceAppResource = null;
        // Inject environment variables for services produced by this executable.
        foreach (var serviceProduced in executableResource.ServicesProduced)
        {
            var name = serviceProduced.Service.Metadata.Name;
            var envVar = serviceProduced.EndpointAnnotation.EnvironmentVariable;

            if (envVar is not null)
            {
                config.Add(envVar, $"{{{{- portForServing \"{name}\" }}}}");
            }

            if (httpsServiceAppResource is null && serviceProduced.EndpointAnnotation.UriScheme == "https")
            {
                httpsServiceAppResource = serviceProduced;
            }
        }

        // REVIEW: If you run as an executable, we don't know that you're an ASP.NET Core application so we don't want to
        // inject ASPNETCORE_HTTPS_PORT.
        if (executableResource.ModelResource is ProjectResource)
        {
            // Add the environment variable for the HTTPS port if we have an HTTPS service. This will make sure the
            // HTTPS redirection middleware avoids redirecting to the internal port.
            if (httpsServiceAppResource is not null)
            {
                config.Add("ASPNETCORE_HTTPS_PORT", $"{{{{- portFor \"{httpsServiceAppResource.Service.Metadata.Name}\" }}}}");
            }
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
                    var volumeSpec = new VolumeMount
                    {
                        Source = isBound && !Path.IsPathRooted(mount.Source) ?
                            Path.GetFullPath(mount.Source) : mount.Source,
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

                var config = new Dictionary<string, string>();

                dcpContainerResource.Spec.Env = new();

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

                        switch (sp.EndpointAnnotation.Protocol)
                        {
                            case ProtocolType.Tcp:
                                portSpec.Protocol = PortProtocol.TCP; break;
                            case ProtocolType.Udp:
                                portSpec.Protocol = PortProtocol.UDP; break;
                        }

                        dcpContainerResource.Spec.Ports.Add(portSpec);

                        var name = sp.Service.Metadata.Name;
                        var envVar = sp.EndpointAnnotation.EnvironmentVariable;

                        if (envVar is not null)
                        {
                            config.Add(envVar, $"{{{{- portForServing \"{name}\" }}}}");
                        }
                    }
                }

                if (modelContainerResource.TryGetEnvironmentVariables(out var containerEnvironmentVariables))
                {
                    var context = new EnvironmentCallbackContext("dcp", config);

                    foreach (var v in containerEnvironmentVariables)
                    {
                        v.Callback(context);
                    }
                }

                foreach (var kvp in config)
                {
                    dcpContainerResource.Spec.Env.Add(new EnvVar { Name = kvp.Key, Value = kvp.Value });
                }

                if (modelContainerResource.TryGetAnnotationsOfType<ExecutableArgsCallbackAnnotation>(out var argsCallback))
                {
                    dcpContainerResource.Spec.Args ??= [];
                    foreach (var callback in argsCallback)
                    {
                        callback.Callback(dcpContainerResource.Spec.Args);
                    }
                }

                if (modelContainerResource is ContainerResource containerResource)
                {
                    dcpContainerResource.Spec.Command = containerResource.Entrypoint;
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
            // Projects/Executables have their ports auto-allocated; the the port specified by the EndpointAnnotation
            // is applied to the Service objects and used by clients.
            // Containers use the port from the EndpointAnnotation directly.

            if (modelResource.IsContainer())
            {
                if (sp.EndpointAnnotation.ContainerPort is null)
                {
                    throw new InvalidOperationException($"The endpoint for container resource {modelResourceName} must specify the ContainerPort");
                }

                sp.DcpServiceProducerAnnotation.Port = sp.EndpointAnnotation.ContainerPort;
            }

            dcpResource.AnnotateAsObjectList(CustomResource.ServiceProducerAnnotation, sp.DcpServiceProducerAnnotation);
            appResource.ServicesProduced.Add(sp);
        }
    }

    private async Task CreateResourcesAsync<RT>(CancellationToken cancellationToken) where RT : CustomResource
    {
        try
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
        catch (OperationCanceledException ex)
        {
            // We catch and suppress the OperationCancelledException because the user may CTRL-C
            // during start up of the resources.
            _logger.LogDebug(ex, "Cancellation during creation of resources.");
        }
    }

    private async Task DeleteResourcesAsync<RT>(string resourceType, CancellationToken cancellationToken) where RT : CustomResource
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
                _logger.LogInformation(ex, "Could not stop {ResourceType} '{ResourceName}'.", resourceType, res.Metadata.Name);
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
