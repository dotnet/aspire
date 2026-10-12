// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Native.Model;

namespace Aspire.Hosting.Native.Runtime;

internal sealed partial class RuntimeGeneration
{
    private Task? _declaredActivation;
    private readonly HashSet<Guid> _declaredOwners = [];
    private Task? _declaredRetirement;
    private Dictionary<Guid, ImmutableArray<Guid>> _retirementDependencies = [];
    private readonly Dictionary<Guid, ProviderLifecycleState> _providerLifecycleStates = [];

    private static async Task RetireDeclaredWorkloadsAsync(Dictionary<Guid, ResourceWorkload> workloads,
        Dictionary<Guid, ImmutableArray<Guid>> dependencies)
    {
        // Consumers can need their producers during retirement. Delete leaves
        // before dependencies, independent of resource names or insertion order.
        while (workloads.Count > 0)
        {
            var leaves = workloads.Keys.Where(id => !dependencies.Any(resource =>
                workloads.ContainsKey(resource.Key) && resource.Value.Contains(id))).ToArray();
            if (leaves.Length == 0)
            {
                throw new InvalidOperationException("The declared retirement graph contains a cycle.");
            }
            await Task.WhenAll(leaves.Select(id => workloads[id].StopAsync())).ConfigureAwait(false);
            foreach (var id in leaves)
            {
                workloads.Remove(id);
            }
        }
    }

    internal void StartDeclaredApplication()
    {
        lock (_gate)
        {
            EnsureAlive();
            if (_declaredActivation is not null)
            {
                throw new InvalidOperationException("The declared application has already started.");
            }
            _declaredActivation = StartDeclaredApplicationAsync();
            _ = _declaredActivation.ContinueWith(static task => _ = task.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }

    internal Task WaitDeclaredApplicationAsync(int timeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeoutMilliseconds, 1);

        return (_declaredActivation ?? throw new InvalidOperationException("The declared application has not started."))
            .WaitAsync(TimeSpan.FromMilliseconds(timeoutMilliseconds), options.TimeProvider);
    }

    internal async Task<bool> WaitDeclaredResourceCompletionAsync(Guid resourceId, int timeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeoutMilliseconds, 1);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMilliseconds), options.TimeProvider);
        while (true)
        {
            Task changed;
            lock (_gate)
            {
                EnsureAlive();
                var resource = GetResource(resourceId);
                if (resource.State.IsTerminal())
                {
                    return resource.State == ResourceState.Stopped;
                }
                changed = _changed.Task;
            }
            await changed.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
    }

    /// <summary>Starts desired workloads concurrently; value dependencies and health gate each individual start.</summary>
    internal async Task StartDeclaredApplicationAsync()
    {
        List<Task<WorkloadEndpoint>> starts = [];
        try
        {
            lock (_gate)
            {
                EnsureAlive();
                foreach (var resource in _resources.Values)
                {
                    if (resource.Declaration.Workload is not { } workload)
                    {
                        continue;
                    }
                    if (resource.Claimed)
                    {
                        throw new InvalidOperationException("A declared workload cannot also have an external execution owner.");
                    }
                    resource.Claimed = true;
                    _declaredOwners.Add(resource.Declaration.Handle.ResourceId);
                    var context = workload.Plan is ContainerWorkload ? ValueNetworkContext.Container : ValueNetworkContext.Host;
                    starts.Add(StartSymbolicWorkload(resource.Declaration.Handle.ResourceId, resource.ConfigurationRevision,
                        workload.Plan, context, workload.Bindings));
                }

            }
            await Task.WhenAll(starts).ConfigureAwait(false);
        }
        catch
        {
            // Some starts can still be waiting on the failed resource. Revoke all
            // declared starts before draining, rather than deadlocking on their DAG.
            Dispose();
            throw;
        }
    }

    private async Task ApplyDeclaredRevisionAsync(Task removedCleanup)
    {
        await removedCleanup.ConfigureAwait(false);
        List<Task<WorkloadEndpoint>> starts = [];
        lock (_gate)
        {
            if (_declaredActivation is null)
            {
                return;
            }
            EnsureAlive();
            _declaredOwners.RemoveWhere(id => !_resources.ContainsKey(id));
            // Compute the affected DAG before scheduling replacements: a consumer
            // may precede its producer in declaration order, but must receive the
            // producer's newly allocated endpoint, never its previous address.
            foreach (var resource in _resources.Values.Where(resource => _declaredOwners.Contains(resource.Declaration.Handle.ResourceId) &&
                resource.AppliedConfigurationRevision != resource.ConfigurationRevision).ToArray())
            {
                InvalidateDependents(resource.Declaration.Handle.ResourceId, true);
            }
            foreach (var resource in _resources.Values)
            {
                var id = resource.Declaration.Handle.ResourceId;
                if (resource.Declaration.Workload is not { } workload)
                {
                    if (_declaredOwners.Contains(id))
                    {
                        throw new InvalidOperationException("A live declared workload cannot change to an external-owner declaration.");
                    }
                    continue;
                }
                var context = workload.Plan is ContainerWorkload ? ValueNetworkContext.Container : ValueNetworkContext.Host;
                if (_declaredOwners.Contains(id))
                {
                    if (resource.AppliedConfigurationRevision != resource.ConfigurationRevision)
                    {
                        starts.Add(ReplaceSymbolicWorkloadAsync(id, resource.ConfigurationRevision,
                            workload.Plan, context, workload.Bindings));
                    }
                }
                else
                {
                    if (resource.Claimed)
                    {
                        throw new InvalidOperationException("An external owner cannot be replaced by declarative ownership.");
                    }
                    resource.Claimed = true;
                    _declaredOwners.Add(id);
                    starts.Add(StartSymbolicWorkload(id, resource.ConfigurationRevision, workload.Plan, context, workload.Bindings));
                }
            }
            _declaredActivation = Task.WhenAll(starts);
        }
        await _declaredActivation.ConfigureAwait(false);
    }

    private (Func<CancellationToken, Task> Initialize,
        Func<WorkloadEndpoint, CancellationToken, Task> Allocated, Func<Task> Retire) CreateLifecycle(Guid resourceId)
    {
        var resource = GetOwnedResource(resourceId);
        var lifecycle = resource.Declaration.Workload?.Lifecycle ?? [];
        var provider = new ProviderLifecycleState(resource.Declaration.ProviderLifecycle);
        _providerLifecycleStates[resourceId] = provider;
        ImmutableDictionary<string, string> retirementValues = ImmutableDictionary<string, string>.Empty;
        ExecutableWorkload? retirement = null;
        var entered = false;
        async Task InvokeProvider(string phase, ImmutableDictionary<string, string> values, CancellationToken token)
        {
            foreach (var binding in provider.Bindings.Where(binding => binding.Phase == phase))
            {
                lock (_gate)
                {
                    provider.ActiveInvocations++;
                }
                using var authority = new ResourceLifecycleAuthority(phase, values, RunLifecycleCommandAsync,
                    (stream, message) => AppendWorkloadLog(resourceId, stream, message), token);
                try
                {
                    await options.ProviderLifecycleDispatcher.InvokeAsync(binding, authority, token).ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        authority.Revoke();
                        await authority.DrainAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        lock (_gate)
                        {
                            provider.ActiveInvocations--;
                        }
                    }
                }
            }
        }
        WorkloadPlan Resolve(DeclaredLifecycleCommand command, bool allocated) =>
            ResolveSymbolicWorkload(resourceId, resource.ConfigurationRevision, command.Plan, ValueNetworkContext.Host,
                command.Bindings, allocated);
        async Task Execute(string phase, bool allocationAvailable, CancellationToken cancellationToken)
        {
            if (lifecycle.SingleOrDefault(command => command.Phase == phase) is not { } command)
            {
                return;
            }
            var plan = Resolve(command, allocationAvailable);
            if (!await RunLifecycleCommandAsync(plan, cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("An integration lifecycle command failed.");
            }
        }
        async Task Allocated(WorkloadEndpoint endpoint, CancellationToken cancellationToken)
        {
            retirementValues = CaptureLifecycleValues(resourceId, true);
            await InvokeProvider("allocated", retirementValues, cancellationToken).ConfigureAwait(false);
            // Resolve retirement while allocation and desired values are still
            // authoritative. Retirement can then run after handle revocation.
            if (lifecycle.SingleOrDefault(command => command.Phase == "retire") is { } retirementCommand)
            {
                retirement = (ExecutableWorkload)Resolve(retirementCommand, true);
            }
            if (lifecycle.SingleOrDefault(command => command.Phase == "health") is { } health)
            {
                var plan = Resolve(health, true);
                while (!await RunLifecycleCommandAsync(plan, cancellationToken).ConfigureAwait(false))
                {
                    await Task.Delay(options.ObservationInterval, options.TimeProvider, cancellationToken).ConfigureAwait(false);
                }
            }
            await Execute("ready", true, cancellationToken).ConfigureAwait(false);
            await InvokeProvider("ready", retirementValues, cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                EnsureAlive();
                resource.Healthy = true;
                resource.AppliedConfigurationRevision = resource.ConfigurationRevision;
                resource.ConfigurationStatus = ConfigurationStatus.Succeeded;
                Changed();
            }
        }
        async Task Retire()
        {
            if (!entered)
            {
                return;
            }
            using var deadline = new CancellationTokenSource(options.WorkloadStartupTimeout, options.TimeProvider);
            try
            {
                if (lifecycle.SingleOrDefault(command => command.Phase == "retire") is not null)
                {
                    if (retirement is null)
                    {
                        throw new InvalidOperationException("The integration retirement command requires an unavailable endpoint allocation.");
                    }
                    if (!await RunLifecycleCommandAsync(retirement, deadline.Token).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("An integration retirement command failed.");
                    }
                }
            }
            finally
            {
                // Public declaration handles have already been revoked. This
                // phase grants only captured native input execution and logging.
                await InvokeProvider("retiring", retirementValues, deadline.Token).ConfigureAwait(false);
            }
        }

        async Task Initialize(CancellationToken token)
        {
            entered = true;
            retirementValues = CaptureLifecycleValues(resourceId, false);
            await InvokeProvider("initialize", retirementValues, token).ConfigureAwait(false);
            if (lifecycle.SingleOrDefault(command => command.Phase == "retire") is { } retirementCommand &&
                retirementCommand.Bindings.Environment.Select(binding => binding.ConfigurationKey)
                    .Concat(retirementCommand.Bindings.Arguments.Select(binding => binding.ConfigurationKey))
                    .All(key => resource.Declaration.SymbolicConfiguration.Single(entry => entry.Name == key).Value.References
                        .All(handle => handle.ResourceId != resourceId)))
            {
                retirement = (ExecutableWorkload)Resolve(retirementCommand, false);
            }
            await Execute("initialize", false, token).ConfigureAwait(false);
        }

        return (Initialize, Allocated, Retire);
    }

    private ImmutableDictionary<string, string> CaptureLifecycleValues(Guid resourceId, bool allocated)
    {
        lock (_gate)
        {
            var values = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
            foreach (var entry in GetOwnedResource(resourceId).Declaration.SymbolicConfiguration)
            {
                if (!allocated && entry.Value.References.Any(handle => handle.ResourceId == resourceId))
                {
                    continue;
                }
                var value = entry.Value.Resolve(ValueNetworkContext.Host,
                    (handle, name, property) => ResolveEndpointValue(handle, name, property, allocated ? resourceId : null));
                if (entry.Value.IsSecret)
                {
                    if (!_secretRedactions.Contains(value) && _secretRedactions.Count >= 4096)
                    {
                        throw new SymbolicValueException(SymbolicValueFailure.SecretHistoryLimit);
                    }
                    _secretRedactions.Add(value);
                }
                values.Add(entry.Name, value);
            }

            return values.ToImmutable();
        }
    }

    private async Task<bool> RunLifecycleCommandAsync(WorkloadPlan plan, CancellationToken cancellationToken)
    {
        // Commands get unique native workload ownership and are always removed.
        // Neither output nor resolved inputs is delivered to an authoring client.
        var lease = await executor.StartAsync(new WorkloadIdentity(GenerationId, Guid.NewGuid()),
            plan, cancellationToken).ConfigureAwait(false);
        await using var cleanup = lease.ConfigureAwait(false);
        while (true)
        {
            var status = await lease.ReadStatusAsync(cancellationToken).ConfigureAwait(false);
            if (status.Phase is WorkloadPhase.Exited or WorkloadPhase.Failed)
            {
                return status.Phase == WorkloadPhase.Exited && status.ExitCode == 0;
            }
            await Task.Delay(options.ObservationInterval, options.TimeProvider, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class ProviderLifecycleState(ImmutableArray<ProviderLifecycleBinding> bindings)
    {
        public ImmutableArray<ProviderLifecycleBinding> Bindings { get; set; } = bindings;
        public int ActiveInvocations { get; set; }
    }
}
