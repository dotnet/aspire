// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Native.Model;
using Aspire.Hosting.Native.Runtime;

namespace Aspire.Hosting.Native.Api;

internal sealed partial class Composition
{
    /// <summary>Declares a container without launching it or acquiring execution ownership.</summary>
    [AspireExport]
    public DeclaredResource AddContainer(string name, string image)
    {
        var plan = DeclaredWorkload.Create(new ContainerWorkload(image, null, [], [], [], []));
        var resource = AddResource(name, "hosting/container");
        Model.SetWorkload(resource.Handle, plan);

        return resource;
    }

    /// <summary>Declares a host executable without launching it.</summary>
    [AspireExport]
    public DeclaredResource AddExecutable(string name, string executablePath, string workingDirectory)
    {
        var plan = DeclaredWorkload.Create(new ExecutableWorkload(executablePath, workingDirectory, [], [], []));
        var resource = AddResource(name, "hosting/executable");
        Model.SetWorkload(resource.Handle, plan);

        return resource;
    }

    /// <summary>Seals the desired graph and starts all declared workloads with native readiness ordering.</summary>
    [AspireExport]
    public ApplicationExecution StartApplication()
    {
        lock (_runtimeGate)
        {
            var execution = _execution ?? CreateExecution();
            execution.Runtime.StartDeclaredApplication();

            return execution;
        }
    }
}

internal sealed partial class DeclaredResource
{
    private DeclaredWorkload ReadWorkload() => Composition.Model.ReadResource(Handle).Workload ??
        throw new InvalidOperationException("The resource has no declared workload.");

    /// <summary>Assigns a late-bound environment value and derives its readiness dependencies.</summary>
    [AspireExport]
    public DeclaredResource WithEnvironment(string name, DeclaredValue value)
    {
        var current = ReadWorkload();
        var key = $"workload.env.{name}";
        var next = current with
        {
            Bindings = current.Bindings with
            {
                Environment = current.Bindings.Environment.Where(binding => binding.Name != name)
                    .Append(new SymbolicEnvironmentBinding(name, key)).ToImmutableArray()
            }
        };
        next.Bindings.Validate(next.Plan);
        SetSymbolicConfiguration(key, value);
        Composition.Model.SetWorkload(Handle, next);

        return this;
    }

    /// <summary>References a symbolic connection value through an environment slot without resolving it.</summary>
    [AspireExport]
    public DeclaredResource WithReference(string environmentName, DeclaredValue value) => WithEnvironment(environmentName, value);

    /// <summary>Appends a late-bound argument to the declared workload.</summary>
    [AspireExport]
    public DeclaredResource WithArgument(DeclaredValue value)
    {
        var current = ReadWorkload();
        var index = current.Plan.Arguments.Length;
        var key = $"workload.arg.{index}";
        var plan = SymbolicWorkloadBindings.Copy(current.Plan, current.Plan.Environment, current.Plan.Arguments.Add(""));
        var next = current with
        {
            Plan = plan,
            Bindings = current.Bindings with { Arguments = current.Bindings.Arguments.Add(new(index, key)) }
        };
        next.Bindings.Validate(plan);
        SetSymbolicConfiguration(key, value);
        Composition.Model.SetWorkload(Handle, next);

        return this;
    }

    /// <summary>Declares a named endpoint; host ports are allocated when the application starts.</summary>
    [AspireExport]
    public DeclaredResource WithEndpoint(WorkloadEndpointDeclaration endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        var current = ReadWorkload();
        var ports = current.Plan.Endpoints.Where(port => port.Name != endpoint.Name).Append(
            new WorkloadPort(endpoint.Name, RuntimeStates.ParseProtocol(endpoint.Protocol), endpoint.UriScheme,
                endpoint.TargetPort, endpoint.HostPort, endpoint.EnvironmentVariable)).ToImmutableArray();
        var plan = current.Plan switch
        {
            ContainerWorkload container => (WorkloadPlan)(container with { Ports = ports }),
            ExecutableWorkload executable => executable with { Ports = ports },
            _ => throw new NotSupportedException("The declared workload primitive is unsupported.")
        };
        Composition.Model.SetWorkload(Handle, current with { Plan = plan });

        return this;
    }

    /// <summary>Configures the container entrypoint without interpreting shell syntax.</summary>
    [AspireExport]
    public DeclaredResource WithEntrypoint(string entrypoint)
    {
        var current = ReadWorkload();
        if (current.Plan is not ContainerWorkload container)
        {
            throw new InvalidOperationException("Entrypoints require a container declaration.");
        }
        Composition.Model.SetWorkload(Handle, current with { Plan = container with { Command = entrypoint } });

        return this;
    }

    /// <summary>Mounts a named retained data volume; application retirement removes the container, not the volume.</summary>
    [AspireExport]
    public DeclaredResource WithVolume(string name, string target, bool readOnly)
    {
        var current = ReadWorkload();
        if (current.Plan is not ContainerWorkload container)
        {
            throw new InvalidOperationException("Volumes require a container declaration.");
        }
        var mounts = container.Mounts.Where(mount => mount.Target != target)
            .Append(new WorkloadMount(WorkloadMountKind.Volume, name, target, readOnly)).ToImmutableArray();
        Composition.Model.SetWorkload(Handle, current with { Plan = container with { Mounts = mounts } });

        return this;
    }

    /// <summary>Declares an integration-owned executable hook for initialize, health, ready, or retire.</summary>
    /// <remarks>Health commands must be side-effect-free and return zero only after authentic protocol readiness.
    /// Inputs reference native symbolic fields; no resolved credentials are returned to integration authors.</remarks>
    [AspireExport]
    public DeclaredResource WithLifecycleExecutable(string phase, ExecutableWorkloadLaunch launch, SymbolicLaunchBindings bindings)
    {
        if (phase is not ("initialize" or "health" or "ready" or "retire"))
        {
            throw new ArgumentException("Unsupported declared lifecycle phase.", nameof(phase));
        }
        var command = ResourceExecution.CreateExecutablePlan(launch);
        var slots = ResourceExecution.CopyBindings(bindings);
        slots.Validate(command);
        var current = ReadWorkload();
        var next = current with
        {
            Lifecycle = current.Lifecycle.Where(hook => hook.Phase != phase)
                .Append(new(phase, command, slots)).ToImmutableArray()
        };
        Composition.Model.SetWorkload(Handle, next);

        return this;
    }
}
