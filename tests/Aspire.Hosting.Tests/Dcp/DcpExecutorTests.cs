// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Channels;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Tests.Utils;
using k8s.Models;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Polly;

namespace Aspire.Hosting.Tests.Dcp;

public class DcpExecutorTests
{
    [Fact]
    public async Task ContainersArePassedOtelServiceName()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("CustomName", "container").WithOtlpExporter();

        var kubernetesService = new TestKubernetesService();

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);

        // Act
        await appExecutor.RunApplicationAsync();

        // Assert
        var container = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.Equal("CustomName", container.Metadata.Annotations["otel-service-name"]);
    }

    [Fact]
    public async Task ResourceStarted_ProjectHasReplicas_EventRaisedOnce()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        var resource = builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithReplicas(2).Resource;

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard", ResourceNameSuffix = "suffix" };

        var startingEvents = new List<OnResourceStartingContext>();
        var events = new DcpExecutorEvents();
        events.Subscribe<OnResourceStartingContext>((context) =>
        {
            startingEvents.Add(context);
            return Task.CompletedTask;
        });

        var channel = Channel.CreateUnbounded<string>();
        events.Subscribe<OnResourceChangedContext>(async (context) =>
        {
            if (context.Resource == resource)
            {
                await channel.Writer.WriteAsync(context.DcpResourceName);
            }
        });

        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions, events: events);
        await appExecutor.RunApplicationAsync();

        var executables = kubernetesService.CreatedResources.OfType<Executable>().ToList();
        Assert.Equal(2, executables.Count);

        var e = Assert.Single(startingEvents);
        Assert.Equal(resource, e.Resource);

        var resourceIds = new HashSet<string>();
        var watchResourceTask = Task.Run(async () =>
        {
            await foreach (var item in channel.Reader.ReadAllAsync())
            {
                resourceIds.Add(item);
                if (resourceIds.Count == 2)
                {
                    break;
                }
            }
        });
        await watchResourceTask.DefaultTimeout();

        Assert.Equal(2, resourceIds.Count);
    }

    [Theory]
    [InlineData(ExecutionType.IDE, false, null, new string[] { "--test1", "--test2" })]
    [InlineData(ExecutionType.IDE, true, new string[] { "--withargs-test" }, new string[] { "--withargs-test" })]
    [InlineData(ExecutionType.Process, false, new string[] { "--test1", "--test2" }, new string[] { "--test1", "--test2" })]
    [InlineData(ExecutionType.Process, true, new string[] { "--", "--test1", "--test2", "--withargs-test" }, new string[] { "--", "--test1", "--test2", "--withargs-test" })]
    public async Task CreateExecutable_LaunchProfileHasCommandLineArgs_AnnotationsAdded(string executionType, bool addAppHostArgs, string[]? expectedArgs, string[]? expectedAnnotations)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        IConfiguration? configuration = null;
        if (executionType == ExecutionType.IDE)
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [DcpExecutor.DebugSessionPortVar] = "8080"
            });

            configuration = configurationBuilder.Build();
        }

        var resourceBuilder = builder.AddProject<Projects.ServiceA>("ServiceA");
        if (addAppHostArgs)
        {
            resourceBuilder
                .WithArgs(c =>
                {
                    c.Args.Add("--withargs-test");
                });
        }

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard", ResourceNameSuffix = "suffix" };

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions, events: events, configuration: configuration);
        await appExecutor.RunApplicationAsync();

        var executables = kubernetesService.CreatedResources.OfType<Executable>().ToList();

        var exe = Assert.Single(executables);

        // Ignore dotnet specific args for .NET project in process execution.
        var callArgs = executionType == ExecutionType.IDE ? exe.Spec.Args : exe.Spec.Args![^(expectedArgs?.Length ?? 0)..];
        Assert.Equal(expectedArgs, callArgs);

        Assert.True(exe.TryGetAnnotationAsObjectList<AppLaunchArgumentAnnotation>(CustomResource.ResourceAppArgsAnnotation, out var argAnnotations));
        Assert.Equal(expectedAnnotations, argAnnotations.Select(a => a.Argument));
    }

    [Fact]
    public async Task ResourceRestarted_EnvironmentCallbacksApplied()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        var callCount = 0;
        var resource = builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithArgs(c =>
            {
                c.Args.Add("--test");
            })
            .WithEnvironment(c =>
            {
                Interlocked.Increment(ref callCount);
                c.EnvironmentVariables["CALL_COUNT"] = callCount.ToString();
            }).Resource;

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard", ResourceNameSuffix = "suffix" };

        var events = new DcpExecutorEvents();
        var resourceNotificationService = ResourceNotificationServiceTestHelpers.Create();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions, events: events);
        await appExecutor.RunApplicationAsync();

        var executables = kubernetesService.CreatedResources.OfType<Executable>().ToList();

        var exe1 = Assert.Single(executables);
        var callCount1 = exe1.Spec.Env!.Single(e => e.Name == "CALL_COUNT");
        Assert.Equal("1", callCount1.Value);

        Assert.Single(exe1.Spec.Args!, a => a == "--no-build");
        Assert.Single(exe1.Spec.Args!, a => a == "--test");
        Assert.True(exe1.TryGetAnnotationAsObjectList<AppLaunchArgumentAnnotation>(CustomResource.ResourceAppArgsAnnotation, out var argAnnotations1));
        Assert.Single(argAnnotations1, a => a.Argument == "--test");

        var reference = appExecutor.GetResource(exe1.Metadata.Name);

        await appExecutor.StopResourceAsync(reference, CancellationToken.None);

        await appExecutor.StartResourceAsync(reference, CancellationToken.None);

        executables = kubernetesService.CreatedResources.OfType<Executable>().ToList();
        Assert.Equal(2, executables.Count);

        var exe2 = executables[1];
        var callCount2 = exe2.Spec.Env!.Single(e => e.Name == "CALL_COUNT");
        Assert.Equal("2", callCount2.Value);

        Assert.Single(exe2.Spec.Args!, a => a == "--no-build");
        Assert.Single(exe2.Spec.Args!, a => a == "--test");
        Assert.True(exe2.TryGetAnnotationAsObjectList<AppLaunchArgumentAnnotation>(CustomResource.ResourceAppArgsAnnotation, out var argAnnotations2));
        Assert.Single(argAnnotations2, a => a.Argument == "--test");
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxiedNoPortNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        var exe = builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "NoPortNoTargetPort", env: "NO_PORT_NO_TARGET_PORT", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Neither Port, nor TargetPort are set
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy gets autogenerated port.
        // Program gets (different) autogenerated port that MUST be injected via env var / startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.True(svc.Status?.EffectivePort >= TestKubernetesService.StartOfAutoPortRange);
        Assert.True(spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port is null,
            "Expected service producer (target) port to not be set (leave allocation to DCP)");
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "NO_PORT_NO_TARGET_PORT").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Contains("""portForServing "CoolProgram" """, envVarVal);
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxiedPortSetNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 1000;
        var exe = builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "PortSetNoTargetPort", port: desiredPort, env: "PORT_SET_NO_TARGET_PORT", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is set, but TargetPort is empty
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy uses Port.
        // Program gets autogenerated port that MUST be injected via env var / startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        Assert.True(spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port is null,
            "Expected service producer (target) port to not be set (leave allocation to DCP)");
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "PORT_SET_NO_TARGET_PORT").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Contains("""portForServing "CoolProgram" """, envVarVal);
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxiedNoPortTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 999;
        var exe = builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "NoPortTargetPortSet", targetPort: desiredPort, env: "NO_PORT_TARGET_PORT_SET", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is empty, TargetPort is set
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy gets autogenerated port.
        // Program uses TargetPort which MAY be injected via env var/ startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.True(svc.Status?.EffectivePort >= TestKubernetesService.StartOfAutoPortRange);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredPort, spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port);
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "NO_PORT_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxiedPortAndTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 998;
        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 997;
        var exe = builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "PortAndTargetPortSet", port: desiredPort, targetPort: desiredTargetPort, env: "PORT_AND_TARGET_PORT_SET", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port and TargetPort set (MUST be different).
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy uses Port.
        // Program uses TargetPort with MAY be injected via env var/ startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port);
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "PORT_AND_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that applying unsupported endpoint port configuration to non-replicated, proxied Executable
    /// results in an error.
    /// </summary>
    [Fact]
    public async Task UnsupportedEndpointPortsExecutableNotReplicatedProxied()
    {
        // Invalid configuration: Port and TargetPort have the same value. This would result in a port conflict.
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 1000;
        builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "EqualPortAndTargetPort", port: desiredPort, targetPort: desiredPort, env: "EQUAL_PORT_AND_TARGET_PORT", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => appExecutor.RunApplicationAsync());
        Assert.Contains("cannot be proxied when both TargetPort and Port are specified with the same value", exception.Message);
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxylessPortSetNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 1000;
        builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "PortSetNoTargetPort", port: desiredPort, env: "PORT_SET_NO_TARGET_PORT", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is set, but TargetPort is empty
        // Clients connect directly to the program, MAY have the program port injected.
        // Program uses TargetPort, which MAY be injected via env var / startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredPort, spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port);
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "PORT_SET_NO_TARGET_PORT").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxylessNoPortTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 999;
        builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "NoPortTargetPortSet", targetPort: desiredPort, env: "NO_PORT_TARGET_PORT_SET", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is empty, TargetPort is set.
        // Clients connect directly to the program, MAY have the program port injected.
        // Program uses TargetPort, which MAY be injected via env var / startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredPort, spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port);
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "NO_PORT_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsExecutableNotReplicatedProxylessPortAndTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 998;
        builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo")
            .WithEndpoint(name: "PortAndTargetPortSet", port: desiredPort, targetPort: desiredPort, env: "PORT_AND_TARGET_PORT_SET", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpExe = Assert.Single(kubernetesService.CreatedResources.OfType<Executable>());
        Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port and target port set (MUST be the same).
        // Clients connect directly to the program, MAY have the program port injected.
        // Program uses TargetPort, which MAY be injected via env var / startup param.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "CoolProgram");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredPort, spAnnList.Single(ann => ann.ServiceName == "CoolProgram").Port);
        var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "PORT_AND_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that applying unsupported endpoint port configuration to non-replicated, proxy-less Executables
    /// results in an error
    /// </summary>
    [Fact]
    public async Task UnsupportedEndpointPortsExecutableNotReplicatedProxyless()
    {
        const int desiredPortOne = TestKubernetesService.StartOfAutoPortRange - 1000;
        const int desiredPortTwo = TestKubernetesService.StartOfAutoPortRange - 999;

        (Action<IResourceBuilder<ExecutableResource>> AddEndpoint, string ErrorMessageFragment)[] testcases = [
            // Note: this configuration (neither Endpoint.Port, nor Endpoint.TargetPort set) COULD be supported as follows:
            // Clients connect directly to the program, MAY have the program port injected.
            // Program gets autogenerated port that MUST be injected via env var/startup param.
            //
            // BUT
            //
            // as of Aspire GA (May 2024) this is not supported due to how Aspire app model consumes autogenerated ports.
            // Namely, the Aspire ApplicationExecutor creates Services and waits for Services to have ports allocated (by DCP)
            // before creating Executables and Containers that implement these services.
            // This does not work for proxy-less Services backed by Executables with auto-generated ports, because these Services
            // get their ports from Executables that are backing them, and those Executables, in turn, get their ports when they get started.
            // Delaying Executable creation like Aspire ApplicationExecutor does means the Services will never get their ports.
            (
                er => er.WithEndpoint(name: "NoPortNoTargetPort", env: "NO_PORT_NO_TARGET_PORT", isProxied: false),
                "needs to specify a port for endpoint"
            ),

            // Invalid configuration: both Port and TargetPort set, but to different values.
            (
                er => er.WithEndpoint(name: "PortAndTargetPortSetDifferently", port: desiredPortOne, targetPort: desiredPortTwo, env: "PORT_AND_TARGET_PORT_SET_DIFFERENTLY", isProxied: false),
                "has a value of Port property that is different from the value of TargetPort property"
            )
        ];

        foreach (var tc in testcases)
        {
            var builder = DistributedApplication.CreateBuilder();

            var exe = builder.AddExecutable("CoolProgram", "cool", Environment.CurrentDirectory, "--alpha", "--bravo");
            tc.AddEndpoint(exe);

            var kubernetesService = new TestKubernetesService();
            using var app = builder.Build();
            var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => appExecutor.RunApplicationAsync());
            Assert.Contains(tc.ErrorMessageFragment, exception.Message);
        }
    }

    [Theory]
    [InlineData(1, "ServiceA")]
    [InlineData(2, "ServiceA")]
    public async Task EndpointOtelServiceName(int replicaCount, string expectedName)
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithReplicas(replicaCount);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard", ResourceNameSuffix = "suffix" };
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions);
        await appExecutor.RunApplicationAsync();

        var executables = kubernetesService.CreatedResources.OfType<Executable>().ToList();
        Assert.Equal(replicaCount, executables.Count);

        foreach (var exe in executables)
        {
            Assert.Equal(expectedName, exe.Metadata.Annotations[CustomResource.OtelServiceNameAnnotation]);
        }
    }

    [Fact]
    public async Task ResourceLogging_MultipleStreams_StreamedOverTime()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        builder.AddContainer("database", "image");

        var logStreamPipesChannel = Channel.CreateUnbounded<(string Type, Pipe Pipe)>();
        var kubernetesService = new TestKubernetesService(startStream: (obj, logStreamType) =>
        {
            var s = new Pipe();
            if (!logStreamPipesChannel.Writer.TryWrite((logStreamType, s)))
            {
                Assert.Fail("Pipe channel unexpectedly closed.");
            }

            return s.Reader.AsStream();
        });
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard" };
        var resourceLoggerService = new ResourceLoggerService();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions, resourceLoggerService: resourceLoggerService);
        await appExecutor.RunApplicationAsync();

        var exeResource = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());

        // Start watching logs for container.
        var watchCts = new CancellationTokenSource();
        var watchSubscribers = resourceLoggerService.WatchAnySubscribersAsync();
        var watchSubscribersEnumerator = watchSubscribers.GetAsyncEnumerator();
        var watchLogs = resourceLoggerService.WatchAsync(exeResource.Metadata.Name);
        var watchLogsEnumerator = watchLogs.GetAsyncEnumerator(watchCts.Token);

        var moveNextTask = watchLogsEnumerator.MoveNextAsync().AsTask();
        Assert.False(moveNextTask.IsCompletedSuccessfully, "No logs yet.");

        await watchSubscribersEnumerator.MoveNextAsync();
        Assert.Equal(exeResource.Metadata.Name, watchSubscribersEnumerator.Current.Name);
        Assert.True(watchSubscribersEnumerator.Current.AnySubscribers);

        exeResource.Status = new ContainerStatus { State = ContainerState.Running };
        kubernetesService.PushResourceModified(exeResource);

        var pipes = await GetStreamPipesAsync(logStreamPipesChannel);

        // Write content to container output stream. This is read by logging and creates log lines.
        await pipes.StandardOut.Writer.WriteAsync(Encoding.UTF8.GetBytes("2024-08-19T06:10:33.473275911Z Hello world" + Environment.NewLine));
        Assert.True(await moveNextTask);
        var logLine = watchLogsEnumerator.Current.Single();
        Assert.Equal("2024-08-19T06:10:33.4732759Z Hello world", logLine.Content);
        Assert.Equal(1, logLine.LineNumber);
        Assert.False(logLine.IsErrorMessage);

        moveNextTask = watchLogsEnumerator.MoveNextAsync().AsTask();
        Assert.False(moveNextTask.IsCompletedSuccessfully, "No logs yet.");

        // Note: This console log is earlier than the previous, but logs are displayed in real time as they're available.
        await pipes.StandardErr.Writer.WriteAsync(Encoding.UTF8.GetBytes("2024-08-19T06:10:32.661Z Next" + Environment.NewLine));
        Assert.True(await moveNextTask);
        logLine = watchLogsEnumerator.Current.Single();
        Assert.Equal("2024-08-19T06:10:32.6610000Z Next", logLine.Content);
        Assert.Equal(2, logLine.LineNumber);
        Assert.True(logLine.IsErrorMessage);

        var loggerState = resourceLoggerService.GetResourceLoggerState(exeResource.Metadata.Name);
        Assert.Collection(loggerState.GetBacklogSnapshot(),
            l => Assert.Equal("Next", l.Content),
            l => Assert.Equal("Hello world", l.Content));

        // Stop watching.
        moveNextTask = watchLogsEnumerator.MoveNextAsync().AsTask();
        watchCts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await moveNextTask);

        await watchSubscribersEnumerator.MoveNextAsync();
        Assert.Equal(exeResource.Metadata.Name, watchSubscribersEnumerator.Current.Name);
        Assert.False(watchSubscribersEnumerator.Current.AnySubscribers);

        // State is clear when no longer watching.
        await AsyncTestHelpers.AssertIsTrueRetryAsync(
            () => loggerState.GetBacklogSnapshot().Length == 0,
            "Backlog is asynchronously cleared after watch ends.");
    }

    [Fact]
    public async Task ResourceLogging_ReplayBacklog_SentInBatch()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        builder.AddContainer("database", "image");

        var kubernetesService = new TestKubernetesService(startStream: (obj, logStreamType) =>
        {
            switch (logStreamType)
            {
                case Logs.StreamTypeStdOut:
                    return new MemoryStream(Encoding.UTF8.GetBytes("2024-08-19T06:10:01.000Z First" + Environment.NewLine));
                case Logs.StreamTypeStdErr:
                    return new MemoryStream(Encoding.UTF8.GetBytes("2024-08-19T06:10:02.000Z Second" + Environment.NewLine));
                case Logs.StreamTypeStartupStdOut:
                    return new MemoryStream(Encoding.UTF8.GetBytes("2024-08-19T06:10:03.000Z Third" + Environment.NewLine));
                case Logs.StreamTypeStartupStdErr:
                    return new MemoryStream(Encoding.UTF8.GetBytes(
                        "2024-08-19T06:10:05.000Z Sixth" + Environment.NewLine +
                        "2024-08-19T06:10:05.000Z Seventh" + Environment.NewLine +
                        "2024-08-19T06:10:04.000Z Forth" + Environment.NewLine +
                        "2024-08-19T06:10:04.000Z Fifth" + Environment.NewLine));
                default:
                    throw new InvalidOperationException("Unexpected type: " + logStreamType);
            }
        });
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpOptions = new DcpOptions { DashboardPath = "./dashboard" };
        var resourceLoggerService = new ResourceLoggerService();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, dcpOptions: dcpOptions, resourceLoggerService: resourceLoggerService);
        await appExecutor.RunApplicationAsync();

        var exeResource = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());

        // Start watching logs for container.
        var watchSubscribers = resourceLoggerService.WatchAnySubscribersAsync();
        var watchSubscribersEnumerator = watchSubscribers.GetAsyncEnumerator();
        var watchLogs1 = resourceLoggerService.WatchAsync(exeResource.Metadata.Name);
        var watchLogsTask1 = ConsoleLoggingTestHelpers.WatchForLogsAsync(watchLogs1, targetLogCount: 7);

        Assert.False(watchLogsTask1.IsCompletedSuccessfully, "Logs not available yet.");

        await watchSubscribersEnumerator.MoveNextAsync();
        Assert.Equal(exeResource.Metadata.Name, watchSubscribersEnumerator.Current.Name);
        Assert.True(watchSubscribersEnumerator.Current.AnySubscribers);

        exeResource.Status = new ContainerStatus { State = ContainerState.Running };
        kubernetesService.PushResourceModified(exeResource);

        var watchLogsResults1 = await watchLogsTask1;
        Assert.Equal(7, watchLogsResults1.Count);
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("First"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Second"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Third"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Forth"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Fifth"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Sixth"));
        Assert.Contains(watchLogsResults1, l => l.Content.Contains("Seventh"));

        var watchLogs2 = resourceLoggerService.WatchAsync(exeResource.Metadata.Name);
        var watchLogsTask2 = ConsoleLoggingTestHelpers.WatchForLogsAsync(watchLogs2, targetLogCount: 7);

        var watchLogsResults2 = await watchLogsTask2;
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("First"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Second"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Third"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Forth"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Fifth"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Sixth"));
        Assert.Contains(watchLogsResults2, l => l.Content.Contains("Seventh"));
    }

    private sealed class LogStreamPipes
    {
        public Pipe StandardOut { get; set; } = default!;
        public Pipe StandardErr { get; set; } = default!;
        public Pipe StartupOut { get; set; } = default!;
        public Pipe StartupErr { get; set; } = default!;
    }

    private static async Task<LogStreamPipes> GetStreamPipesAsync(Channel<(string Type, Pipe Pipe)> logStreamPipesChannel)
    {
        var pipeCount = 0;
        var result = new LogStreamPipes();

        await foreach (var item in logStreamPipesChannel.Reader.ReadAllAsync())
        {
            switch (item.Type)
            {
                case Logs.StreamTypeStdOut:
                    result.StandardOut = item.Pipe;
                    break;
                case Logs.StreamTypeStdErr:
                    result.StandardErr = item.Pipe;
                    break;
                case Logs.StreamTypeStartupStdOut:
                    result.StartupOut = item.Pipe;
                    break;
                case Logs.StreamTypeStartupStdErr:
                    result.StartupErr = item.Pipe;
                    break;
                default:
                    throw new InvalidOperationException("Unexpected type: " + item.Type);
            }

            pipeCount++;
            if (pipeCount == 4)
            {
                logStreamPipesChannel.Writer.Complete();
            }
        }

        return result;
    }

    [Fact]
    public async Task EndpointPortsProjectNoPortNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithEndpoint(name: "NoPortNoTargetPort", env: "NO_PORT_NO_TARGET_PORT", isProxied: true)
            .WithHttpEndpoint(name: "hp1", port: 5001)
            .WithHttpEndpoint(name: "dontinjectme", port: 5002)
            .WithEndpointsInEnvironment(e => e.Name != "dontinjectme")
            .WithReplicas(3);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var exes = kubernetesService.CreatedResources.OfType<Executable>().ToList();
        Assert.Equal(3, exes.Count);

        foreach (var dcpExe in exes)
        {
            Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

            // Neither Port, nor TargetPort are set
            // Clients use proxy, MAY have the proxy port injected.
            // Proxy gets autogenerated port.
            // Each replica gets a different autogenerated port that MUST be injected via env var/startup param.
            var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "ServiceA-NoPortNoTargetPort");
            Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
            Assert.True(svc.Status?.EffectivePort >= TestKubernetesService.StartOfAutoPortRange);
            Assert.True(spAnnList.Single(ann => ann.ServiceName == "ServiceA-NoPortNoTargetPort").Port is null,
                "Expected service producer (target) port to not be set (leave allocation to DCP)");
            var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "NO_PORT_NO_TARGET_PORT").Value;
            Assert.False(string.IsNullOrWhiteSpace(envVarVal));
            Assert.Contains("""portForServing "ServiceA-NoPortNoTargetPort" """, envVarVal);

            // ASPNETCORE_URLS should not include dontinjectme, as it was excluded using WithEndpointsInEnvironment
            var aspnetCoreUrls = dcpExe.Spec.Env?.Single(v => v.Name == "ASPNETCORE_URLS").Value;
            Assert.Equal("http://localhost:{{- portForServing \"ServiceA-http\" -}};http://localhost:{{- portForServing \"ServiceA-hp1\" -}}", aspnetCoreUrls);
        }
    }

    [Fact]
    public async Task EndpointPortsProjectPortSetNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            AssemblyName = typeof(DistributedApplicationTests).Assembly.FullName
        });

        const int desiredPortOne = TestKubernetesService.StartOfAutoPortRange - 1000;
        builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithEndpoint(name: "PortSetNoTargetPort", port: desiredPortOne, env: "PORT_SET_NO_TARGET_PORT", isProxied: true)
            .WithReplicas(3);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var exes = kubernetesService.CreatedResources.OfType<Executable>().ToList();
        Assert.Equal(3, exes.Count);

        foreach (var dcpExe in exes)
        {
            Assert.True(dcpExe.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

            // Port is set, but TargetPort is empty.
            // Clients use proxy, MAY have the proxy port injected.
            // Proxy uses Port.
            // Each replica gets a different autogenerated port that MUST be injected via env var/startup param.
            var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "ServiceA-PortSetNoTargetPort");
            Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
            Assert.Equal(desiredPortOne, svc.Status?.EffectivePort);
            Assert.True(spAnnList.Single(ann => ann.ServiceName == "ServiceA-PortSetNoTargetPort").Port is null,
                "Expected service producer (target) port to not be set (leave allocation to DCP)");
            var envVarVal = dcpExe.Spec.Env?.Single(v => v.Name == "PORT_SET_NO_TARGET_PORT").Value;
            Assert.False(string.IsNullOrWhiteSpace(envVarVal));
            Assert.Contains("""portForServing "ServiceA-PortSetNoTargetPort" """, envVarVal);
        }
    }

    [Fact]
    public async Task EndpointPortsConainerProxiedNoPortTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 999;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "NoPortTargetPortSet", targetPort: desiredTargetPort, env: "NO_PORT_TARGET_PORT_SET", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is empty, TargetPort is set
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy gets autogenerated port.
        // Container is using TargetPort inside the container. Container host port is auto-allocated by Docker/Podman.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.True(svc.Status?.EffectivePort >= TestKubernetesService.StartOfAutoPortRange);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p => p.HostPort is null && p.ContainerPort == desiredTargetPort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "NO_PORT_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsConainerProxiedPortAndTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 998;
        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 997;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "PortAndTargetPortSet", port: desiredPort, targetPort: desiredTargetPort, env: "PORT_AND_TARGET_PORT_SET", isProxied: true);

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port and TargetPort are set.
        // Clients use proxy, MAY have the proxy port injected.
        // Proxy uses Port.
        // Container is using TargetPort inside the container. Container host port is auto-allocated by Docker/Podman.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Localhost, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p => p.HostPort is null && p.ContainerPort == desiredTargetPort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "PORT_AND_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that applying unsupported endpoint port configuration to Containers results in an error.
    /// </summary>
    [Fact]
    public async Task UnsupportedEndpointPortsContainer()
    {
        const int desiredPortOne = TestKubernetesService.StartOfAutoPortRange - 1000;

        (Action<IResourceBuilder<ContainerResource>> AddEndpoint, string ErrorMessageFragment)[] testcases = [
            // Invalid configuration: TargetPort is empty (and Port too) (proxied).
            (
                cr => cr.WithEndpoint(name: "NoPortNoTargetPortProxied", env: "NO_PORT_NO_TARGET_PORT_PROXIED", isProxied: true),
                "must specify the TargetPort"
            ),

            // Invalid configuration: TargetPort is empty (Port is set but it should not matter) (proxied).
            (
                cr => cr.WithEndpoint(name: "PortSetNoTargetPort", port: desiredPortOne, env: "PORT_SET_NO_TARGET_PORT", isProxied: true),
                "must specify the TargetPort"
            ),

            // Invalid configuration: TargetPort is empty (and Port too) (proxy-less).
            (
                cr => cr.WithEndpoint(name: "NoPortNoTargetPortProxyless", env: "NO_PORT_NO_TARGET_PORT_PROXYLESS", isProxied: false),
                "must specify the TargetPort"
            ),
        ];

        foreach (var tc in testcases)
        {
            var builder = DistributedApplication.CreateBuilder();

            var ctr = builder.AddContainer("database", "image");
            tc.AddEndpoint(ctr);

            var kubernetesService = new TestKubernetesService();
            using var app = builder.Build();
            var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
            var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => appExecutor.RunApplicationAsync());
            Assert.Contains(tc.ErrorMessageFragment, exception.Message);
        }
    }

    [Fact]
    public async Task EndpointPortsContainerProxylessPortSetNoTargetPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 1000;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "PortSetNoTargetPort", port: desiredPort, env: "PORT_SET_NO_TARGET_PORT", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Neither Port, nor TargetPort are set.
        // Clients connect directly to the container host port, MAY have the container host port injected.
        // Container is using TargetPort for BOTH listening inside the container and as a host port.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p => p.HostPort == desiredPort && p.ContainerPort == desiredPort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "PORT_SET_NO_TARGET_PORT").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsContainerProxylessNoPortTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 999;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "NoPortTargetPortSet", targetPort: desiredTargetPort, env: "NO_PORT_TARGET_PORT_SET", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port is empty, TargetPort is set
        // Clients connect directly to the container host port, MAY have the container host port injected.
        // Container is using TargetPort for BOTH listening inside the container and as a host port.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredTargetPort, svc.Status?.EffectivePort);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p => p.HostPort == desiredTargetPort && p.ContainerPort == desiredTargetPort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "NO_PORT_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsContainerProxylessPortAndTargetPortSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 998;
        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 997;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "PortAndTargetPortSet", port: desiredPort, targetPort: desiredTargetPort, env: "PORT_AND_TARGET_PORT_SET", isProxied: false);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port and TargetPort are set.
        // Clients connect directly to the container host port, MAY have the container host port injected.
        // Container is using TargetPort for listening inside the container and the Port as the host port.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p => p.HostPort == desiredPort && p.ContainerPort == desiredTargetPort);
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "PORT_AND_TARGET_PORT_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task EndpointPortsContainerProxylessProtocolSet()
    {
        var builder = DistributedApplication.CreateBuilder();

        const int desiredPort = TestKubernetesService.StartOfAutoPortRange - 998;
        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 997;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "PortAndProtocolSet", port: desiredPort, targetPort: desiredTargetPort, env: "PORT_AND_PROTOCOL_SET", isProxied: false, protocol: System.Net.Sockets.ProtocolType.Udp);

        // All these configurations are effectively the same because EndpointAnnotation constructor for proxy-less endpoints
        // will make sure Port and TargetPort have the same value if one is specified but the other is not.

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());
        Assert.True(dcpCtr.TryGetAnnotationAsObjectList<ServiceProducerAnnotation>(CustomResource.ServiceProducerAnnotation, out var spAnnList));

        // Port and TargetPort are set.
        // Clients connect directly to the container host port, MAY have the container host port injected.
        // Container is using TargetPort for listening inside the container and the Port as the host port.
        var svc = kubernetesService.CreatedResources.OfType<Service>().Single(s => s.Name() == "database");
        Assert.Equal(AddressAllocationModes.Proxyless, svc.Spec.AddressAllocationMode);
        Assert.Equal(desiredPort, svc.Status?.EffectivePort);
        Assert.NotNull(dcpCtr.Spec.Ports);
        Assert.Contains(dcpCtr.Spec.Ports!, p =>  p.HostPort == desiredPort && p.ContainerPort == desiredTargetPort && p.Protocol == "UDP");
        // Desired port should be part of the service producer annotation.
        Assert.Equal(desiredTargetPort, spAnnList.Single(ann => ann.ServiceName == "database").Port);
        var envVarVal = dcpCtr.Spec.Env?.Single(v => v.Name == "PORT_AND_PROTOCOL_SET").Value;
        Assert.False(string.IsNullOrWhiteSpace(envVarVal));
        Assert.Equal(desiredTargetPort, int.Parse(envVarVal, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ErrorIfResourceNotDeletedBeforeRestart()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("database", "image");

        var kubernetesService = new TestKubernetesService(ignoreDeletes: true);
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpEvents = new DcpExecutorEvents();
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        dcpEvents.Subscribe<OnResourceFailedToStartContext>(c =>
        {
            tcs.SetResult();
            return Task.CompletedTask;
        });

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, events: dcpEvents);

        // Set a custom pipeline without retries or delays to avoid waiting.
        appExecutor.DeleteResourceRetryPipeline = new ResiliencePipelineBuilder<bool>().Build();

        await appExecutor.RunApplicationAsync();

        var dcpCtr = Assert.Single(kubernetesService.CreatedResources.OfType<Container>());

        var resourceReference = appExecutor.GetResource(dcpCtr.Metadata.Name);

        var ex = await Assert.ThrowsAsync<DistributedApplicationException>(async () => await appExecutor.StartResourceAsync(resourceReference, CancellationToken.None));
        Assert.Equal($"Failed to delete '{dcpCtr.Metadata.Name}' successfully before restart.", ex.Message);

        // Verify failed to start event.
        await tcs.Task.DefaultTimeout();
    }

    [Fact]
    public async Task AddsDefaultsCommandsToResources()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("database", "image");
        var exe = builder.AddExecutable("node", "node.exe", ".");
        var project = builder.AddProject<TestProject>("project");

        var kubernetesService = new TestKubernetesService();
        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);
        await appExecutor.RunApplicationAsync();

        HasKnownCommandAnnotations(exe.Resource);
        HasKnownCommandAnnotations(container.Resource);
        HasKnownCommandAnnotations(project.Resource);
    }

    [Fact]
    public async Task ContainersArePassedExpectedImagePullPolicy()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("ImplicitDefault", "container");
        builder.AddContainer("ExplicitDefault", "container").WithImagePullPolicy(ImagePullPolicy.Default);
        builder.AddContainer("ExplicitAlways", "container").WithImagePullPolicy(ImagePullPolicy.Always);
        builder.AddContainer("ExplicitMissing", "container").WithImagePullPolicy(ImagePullPolicy.Missing);

        var kubernetesService = new TestKubernetesService();

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService);

        // Act
        await appExecutor.RunApplicationAsync();

        // Assert
        Assert.Equal(4, kubernetesService.CreatedResources.OfType<Container>().Count());
        var implicitDefaultContainer = Assert.Single(kubernetesService.CreatedResources.OfType<Container>(), c => c.AppModelResourceName == "ImplicitDefault");
        Assert.Null(implicitDefaultContainer.Spec.PullPolicy);

        var explicitDefaultContainer = Assert.Single(kubernetesService.CreatedResources.OfType<Container>(), c => c.AppModelResourceName == "ExplicitDefault");
        Assert.Null(explicitDefaultContainer.Spec.PullPolicy);

        var explicitAlwaysContainer = Assert.Single(kubernetesService.CreatedResources.OfType<Container>(), c => c.AppModelResourceName == "ExplicitAlways");
        Assert.Equal(ContainerPullPolicy.Always, explicitAlwaysContainer.Spec.PullPolicy);

        var explicitMissingContainer = Assert.Single(kubernetesService.CreatedResources.OfType<Container>(), c => c.AppModelResourceName == "ExplicitMissing");
        Assert.Equal(ContainerPullPolicy.Missing, explicitMissingContainer.Spec.PullPolicy);
    }

    [Fact]
    public async Task CancelTokenDuringStartup()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        const int desiredTargetPort = TestKubernetesService.StartOfAutoPortRange - 999;
        builder.AddContainer("database", "image")
            .WithEndpoint(name: "NoPortTargetPortSet", targetPort: desiredTargetPort, env: "NO_PORT_TARGET_PORT_SET", isProxied: true);

        var kubernetesService = new TestKubernetesService();

        using var app = builder.Build();
        var distributedAppModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var dcpEvents = new DcpExecutorEvents();
        var tokenSource = new CancellationTokenSource();
        dcpEvents.Subscribe<OnResourcesPreparedContext>((context) =>
        {
            tokenSource.Cancel();
            return Task.CompletedTask;
        });

        var appExecutor = CreateAppExecutor(distributedAppModel, kubernetesService: kubernetesService, events: dcpEvents);

        // Act
        await appExecutor.RunApplicationAsync(tokenSource.Token);

        // Assert
        Assert.True(tokenSource.IsCancellationRequested);
    }

    private static void HasKnownCommandAnnotations(IResource resource)
    {
        var commandAnnotations = resource.Annotations.OfType<ResourceCommandAnnotation>().ToList();
        Assert.Collection(commandAnnotations,
            a => Assert.Equal(KnownResourceCommands.StartCommand, a.Name),
            a => Assert.Equal(KnownResourceCommands.StopCommand, a.Name),
            a => Assert.Equal(KnownResourceCommands.RestartCommand, a.Name));
    }

    private static DcpExecutor CreateAppExecutor(
        DistributedApplicationModel distributedAppModel,
        IHostEnvironment? hostEnvironment = null,
        IConfiguration? configuration = null,
        IKubernetesService? kubernetesService = null,
        DcpOptions? dcpOptions = null,
        ResourceLoggerService? resourceLoggerService = null,
        DcpExecutorEvents? events = null)
    {
        if (configuration == null)
        {
            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [KnownConfigNames.DashboardOtlpGrpcEndpointUrl] = "http://localhost",
                ["AppHost:BrowserToken"] = "TestBrowserToken!",
                ["AppHost:OtlpApiKey"] = "TestOtlpApiKey!"
            });

            configuration = builder.Build();
        }

        resourceLoggerService ??= new ResourceLoggerService();
        dcpOptions ??= new DcpOptions { DashboardPath = "./dashboard" };

        return new DcpExecutor(
            NullLogger<DcpExecutor>.Instance,
            NullLogger<DistributedApplication>.Instance,
            distributedAppModel,
            hostEnvironment ?? new TestHostEnvironment(),
            kubernetesService ?? new TestKubernetesService(),
            configuration,
            new Hosting.Eventing.DistributedApplicationEventing(),
            new DistributedApplicationOptions(),
            Options.Create(dcpOptions),
            new DistributedApplicationExecutionContext(new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Run)
            {
                ServiceProvider = TestServiceProvider.Instance
            }),
            resourceLoggerService,
            new TestDcpDependencyCheckService(),
            new DcpNameGenerator(configuration, Options.Create(dcpOptions)),
            events ?? new DcpExecutorEvents());
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = default!;
        public IFileProvider ContentRootFileProvider { get; set; } = default!;
        public string ContentRootPath { get; set; } = default!;
        public string EnvironmentName { get; set; } = default!;
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "TestProject";
        public LaunchSettings LaunchSettings { get; } = new();
    }

    private sealed class CustomChildResource(string name, IResource parent) : Resource(name), IResourceWithParent
    {
        public IResource Parent => parent;
    }
}
