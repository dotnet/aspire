// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json;
using Aspire.Hosting.Native.Runtime;
using Aspire.Hosting.Native.Server;

namespace Aspire.Hosting.Native.Dcp;

/// <summary>Projects standard native execution primitives to DCP-owned workloads.</summary>
internal sealed class DcpWorkloadExecutor(DcpApiClient client, Task startup, NativeServerOptions options) : IWorkloadExecutor
{
    public DcpWorkloadExecutor(DcpApiClient client) : this(client, Task.CompletedTask, new())
    {
    }

    public DcpWorkloadExecutor(DcpApiClient client, Task startup) : this(client, startup, new())
    {
    }

    public async Task<IWorkloadLease> StartAsync(WorkloadIdentity identity, WorkloadPlan plan, CancellationToken cancellationToken)
    {
        WorkloadDefinition.Validate(plan);
        await startup.WaitAsync(cancellationToken).ConfigureAwait(false);
        var name = "native-" + identity.ResourceId.ToString("N");
        var collection = plan is ContainerWorkload ? "containers" : "executables";
        var services = plan.Endpoints.Select(endpoint => name + "-" + endpoint.Name.ToLowerInvariant()).ToArray();
        var lease = new Lease(client, collection, name, services, options);
        try
        {
            var spec = new DcpWorkloadSpec
            {
                Env = plan.Environment.Select(variable => new DcpEnvironment(variable.Name, variable.Value)).ToArray(),
                Args = plan.Arguments.ToArray()
            };
            Dictionary<string, string>? annotations = null;
            if (!plan.Endpoints.IsEmpty)
            {
                for (var index = 0; index < plan.Endpoints.Length; index++)
                {
                    var endpoint = plan.Endpoints[index];
                    await client.CreateAsync("services", services[index], JsonSerializer.SerializeToElement(
                        new DcpServiceSpec(endpoint.Protocol, plan is ContainerWorkload ? "Proxyless" : "Localhost")
                        {
                            Port = endpoint.HostPort == 0 ? null : endpoint.HostPort
                        },
                        DcpJsonContext.Default.DcpServiceSpec), null, cancellationToken).ConfigureAwait(false);
                }
                annotations = new Dictionary<string, string>
                {
                    ["service-producer"] = JsonSerializer.Serialize(plan.Endpoints.Select((endpoint, index) =>
                        new DcpServiceProducer(services[index], System.Net.IPAddress.Loopback.ToString(),
                            endpoint.TargetPort == 0 ? null : endpoint.TargetPort)).ToArray(),
                        DcpJsonContext.Default.DcpServiceProducerArray)
                };
            }
            if (plan is ContainerWorkload container)
            {
                spec = spec with
                {
                    Image = container.Image,
                    Command = container.Command,
                    VolumeMounts = container.Mounts.IsEmpty ? null :
                        container.Mounts.Select(mount => new DcpVolumeMount(mount.Type, mount.Source, mount.Target, mount.ReadOnly)).ToArray(),
                    Ports = container.Endpoints.IsEmpty ? null : container.Endpoints.Select(endpoint =>
                        new DcpContainerPort(endpoint.TargetPort, System.Net.IPAddress.Loopback.ToString(), endpoint.Protocol)
                        {
                            HostPort = endpoint.HostPort == 0 ? null : endpoint.HostPort
                        }).ToArray()
                };
            }
            else if (plan is ExecutableWorkload executable)
            {
                spec = spec with
                {
                    ExecutablePath = executable.ExecutablePath,
                    WorkingDirectory = executable.WorkingDirectory,
                    ExecutionType = "Process"
                };
                // DCP substitutes "{{- portForServing \"<service>\" -}}" before
                // launch. Each environment variable names its own allocation,
                // avoiding reserve/release races for multi-endpoint executables.
                spec = spec with
                {
                    Env = [.. spec.Env, .. executable.Endpoints.Select((endpoint, index) => (endpoint, index))
                        .Where(entry => entry.endpoint.EnvironmentVariable.Length > 0)
                        .Select(entry => new DcpEnvironment(entry.endpoint.EnvironmentVariable,
                            "{{- portForServing \"" + services[entry.index] + "\" -}}"))]
                };
            }
            else
            {
                throw new NotSupportedException("The workload primitive is not implemented.");
            }
            await client.CreateAsync(collection, name, JsonSerializer.SerializeToElement(spec, DcpJsonContext.Default.DcpWorkloadSpec),
                annotations, cancellationToken).ConfigureAwait(false);
            while (true)
            {
                var resource = await client.GetAsync(collection, name, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("DCP removed the workload during startup.");
                var state = resource.Status?.State;
                if (state is "Exited" or "Finished" or "FailedToStart")
                {
                    throw new WorkloadStartupException("DCP could not start the workload.",
                        await lease.ReadLogsAsync(cancellationToken).ConfigureAwait(false));
                }
                if (state == "Running")
                {
                    var instanceId = plan is ContainerWorkload
                        ? resource.Status?.ContainerId
                        : resource.Status?.Pid?.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (string.IsNullOrEmpty(instanceId))
                    {
                        throw new InvalidOperationException("DCP did not report its running workload identity.");
                    }
                    if (plan.Endpoints.IsEmpty)
                    {
                        lease.Endpoint = new WorkloadEndpoint(System.Net.IPAddress.Loopback.ToString(), 0, instanceId);
                        return lease;
                    }
                    var endpoints = ImmutableArray.CreateBuilder<AllocatedWorkloadPort>();
                    for (var index = 0; index < services.Length; index++)
                    {
                        var endpoint = await client.GetAsync("services", services[index], cancellationToken).ConfigureAwait(false);
                        if (endpoint?.Status is not { EffectivePort: > 0 and <= 65535 } status)
                        {
                            break;
                        }
                        var address = status.EffectiveAddress
                            ?? throw new InvalidDataException("DCP did not report an allocated endpoint address.");
                        if (!address.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                            (!System.Net.IPAddress.TryParse(address, out var host) || !System.Net.IPAddress.IsLoopback(host)))
                        {
                            throw new InvalidOperationException("DCP must allocate a loopback workload endpoint.");
                        }
                        var declaration = plan.Endpoints[index];
                        endpoints.Add(new(declaration.Name, declaration.Protocol, declaration.UriScheme, address, status.EffectivePort.Value));
                    }
                    if (endpoints.Count == services.Length)
                    {
                        lease.Endpoint = new WorkloadEndpoint(instanceId, endpoints.ToImmutable());
                        return lease;
                    }
                }
                await Task.Delay(options.RetryInterval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // A partially submitted workload still has a cleanup obligation.
            try
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                throw new WorkloadCleanupException(lease, exception);
            }
            throw;
        }
    }

    private sealed class Lease(DcpApiClient client, string collection, string name, string[] services,
        NativeServerOptions options) : IWorkloadLease
    {
        private long _stdout;
        private long _stderr;
        private readonly SemaphoreSlim _cleanupGate = new(1);
        private bool _disposed;
        public WorkloadEndpoint Endpoint { get; set; } = new(System.Net.IPAddress.Loopback.ToString(), 0, "");

        public async Task<WorkloadStatus> ReadStatusAsync(CancellationToken cancellationToken)
        {
            var resource = await client.GetAsync(collection, name, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The DCP workload disappeared before cleanup.");
            var state = resource.Status?.State;
            var exitCode = resource.Status?.ExitCode;

            return new WorkloadStatus(state == "Running", exitCode);
        }

        public async Task<ImmutableArray<WorkloadLog>> ReadLogsAsync(CancellationToken cancellationToken)
        {
            var stdout = await client.ReadLogsAsync(collection, name, "stdout", _stdout, cancellationToken).ConfigureAwait(false);
            var stderr = await client.ReadLogsAsync(collection, name, "stderr", _stderr, cancellationToken).ConfigureAwait(false);
            _stdout += stdout.Length;
            _stderr += stderr.Length;

            return stdout.Select(message => new WorkloadLog("stdout", message.TrimEnd('\r')))
                .Concat(stderr.Select(message => new WorkloadLog("stderr", message.TrimEnd('\r')))).ToImmutableArray();
        }

        public async ValueTask DisposeAsync()
        {
            using var cleanup = new CancellationTokenSource(options.CleanupTimeout);
            await _cleanupGate.WaitAsync(cleanup.Token).ConfigureAwait(false);
            try
            {
                if (_disposed)
                {
                    return;
                }
                // A failed DELETE or disappearance probe retains the obligation.
                // Repeating deletion after partial success is safe: DCP treats 404 as absent.
                await client.DeleteAsync(collection, name, cleanup.Token).ConfigureAwait(false);
                foreach (var service in services)
                {
                    await client.DeleteAsync("services", service, cleanup.Token).ConfigureAwait(false);
                }
                _disposed = true;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"DCP workload cleanup failed ({exception.GetType().Name}); cleanup remains pending.");
                throw;
            }
            finally
            {
                _cleanupGate.Release();
            }
        }
    }
}
