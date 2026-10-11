// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Native.Diagnostics;

namespace Aspire.Hosting.Native.Runtime;

/// <summary>Describes a standard execution primitive without integration clients or transport-specific data.</summary>
internal abstract record WorkloadPlan
{
    public abstract ImmutableArray<WorkloadEnvironment> Environment { get; }
    public abstract ImmutableArray<string> Arguments { get; }
    public abstract ImmutableArray<WorkloadPort> Endpoints { get; }
}
internal sealed record ContainerWorkload(string Image, string? Command, ImmutableArray<WorkloadMount> Mounts,
    ImmutableArray<WorkloadEnvironment> Variables, ImmutableArray<string> Args, ImmutableArray<WorkloadPort> Ports)
    : WorkloadPlan
{
    public override ImmutableArray<WorkloadEnvironment> Environment => Variables;
    public override ImmutableArray<string> Arguments => Args;
    public override ImmutableArray<WorkloadPort> Endpoints => Ports;
    public ContainerWorkload(string image, int targetPort, ImmutableArray<WorkloadEnvironment> variables,
        ImmutableArray<string> args) : this(image, null, [], variables, args,
            targetPort == 0 ? [] : [new("tcp", "TCP", "tcp", targetPort, 0, "")])
    {
    }
}
internal sealed record ExecutableWorkload(string ExecutablePath, string WorkingDirectory,
    ImmutableArray<WorkloadEnvironment> Variables, ImmutableArray<string> Args, ImmutableArray<WorkloadPort> Ports)
    : WorkloadPlan
{
    public override ImmutableArray<WorkloadEnvironment> Environment => Variables;
    public override ImmutableArray<string> Arguments => Args;
    public override ImmutableArray<WorkloadPort> Endpoints => Ports;
    public ExecutableWorkload(string executablePath, string workingDirectory, string portEnvironmentVariable,
        ImmutableArray<WorkloadEnvironment> variables, ImmutableArray<string> args) : this(executablePath, workingDirectory,
            variables, args, portEnvironmentVariable.Length == 0 ? [] : [new("tcp", "TCP", "tcp", 0, 0, portEnvironmentVariable)])
    {
    }
}
internal sealed record WorkloadEnvironment(string Name, string Value);
internal sealed record WorkloadIdentity(Guid GenerationId, Guid ResourceId);
internal sealed record WorkloadEndpoint(string InstanceId, ImmutableArray<AllocatedWorkloadPort> Endpoints)
{
    public string Host => Endpoints.IsEmpty ? System.Net.IPAddress.Loopback.ToString() : Endpoints[0].Host;
    public int Port => Endpoints.IsEmpty ? 0 : Endpoints[0].Port;

    public WorkloadEndpoint(string host, int port, string instanceId) : this(instanceId,
        port == 0 ? [] : [new("tcp", "TCP", "tcp", host, port)])
    {
    }
}
internal sealed record WorkloadStatus(bool Running, int? ExitCode);
internal sealed record WorkloadLog(string Stream, string Message);

/// <summary>Preserves startup console diagnostics separately from classified host errors.</summary>
internal sealed class WorkloadStartupException(string message, ImmutableArray<WorkloadLog> logs) : InvalidOperationException(message)
{
    public ImmutableArray<WorkloadLog> Logs { get; } = logs;
}

/// <summary>Retains an executor's partially created workload when startup cleanup fails.</summary>
internal sealed class WorkloadCleanupException(IWorkloadLease lease, Exception failure) :
    InvalidOperationException("Workload startup cleanup remains pending.", failure)
{
    public IWorkloadLease Lease { get; } = lease;
}

/// <summary>Separates orchestration I/O from execution ownership and observations.</summary>
internal interface IWorkloadExecutor
{
    Task<IWorkloadLease> StartAsync(WorkloadIdentity identity, WorkloadPlan plan, CancellationToken cancellationToken);
}

/// <summary>Represents one externally running workload and its cleanup obligation.</summary>
internal interface IWorkloadLease : IAsyncDisposable
{
    WorkloadEndpoint Endpoint { get; }
    Task<WorkloadStatus> ReadStatusAsync(CancellationToken cancellationToken);
    Task<ImmutableArray<WorkloadLog>> ReadLogsAsync(CancellationToken cancellationToken);
}

/// <summary>Rejects execution explicitly in model-only fixtures.</summary>
internal sealed class UnavailableWorkloadExecutor : IWorkloadExecutor
{
    public static UnavailableWorkloadExecutor Instance { get; } = new();
    public Task<IWorkloadLease> StartAsync(WorkloadIdentity identity, WorkloadPlan plan, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No workload executor is configured for this native server.");
}

/// <summary>Owns startup, observation, cancellation, and cleanup for one resource execution.</summary>
internal sealed class ResourceWorkload(
    WorkloadIdentity identity,
    WorkloadPlan plan,
    IWorkloadExecutor executor,
    NativeRuntimeOptions options,
    Func<CancellationToken, Task> waitForDependencies,
    Action<string, WorkloadEndpoint?> publish,
    Action<string, string> log) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    private readonly TaskCompletionSource<WorkloadEndpoint> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Task? _completion;
    private Task? _stop;
    private IWorkloadLease? _lease;
    private bool _cleanupFailed;
    private readonly Lock _lifetimeGate = new();
    private bool _finished;
    public Task Completion => _stop ?? _completion ?? Task.CompletedTask;

    /// <summary>Stops execution and retries a cleanup obligation left by a failed attempt.</summary>
    public Task StopAsync()
    {
        Dispose();
        lock (_lifetimeGate)
        {
            if (_stop is null || _stop.IsFaulted || _stop.IsCanceled)
            {
                _stop = StopCoreAsync();
            }

            return _stop;
        }
    }

    private async Task StopCoreAsync()
    {
        try
        {
            await (_completion ?? Task.CompletedTask).ConfigureAwait(false);
        }
        catch
        {
            if (_cleanupFailed)
            {
                // Completion records the first attempt, not proof that DCP is gone.
                // Retirement must invoke the retained lease rather than reobserve that fault.
                await CleanupAsync().ConfigureAwait(false);
            }
            // Startup/observation failures have already been reported to the owner
            // and resource logs. They must not fence retirement once cleanup succeeds.
        }
    }

    private async Task CleanupAsync()
    {
        if (_lease is null)
        {
            return;
        }
        try
        {
            await _lease.DisposeAsync().ConfigureAwait(false);
            _lease = null;
            _cleanupFailed = false;
        }
        catch (Exception exception)
        {
            _cleanupFailed = true;
            log("stderr", $"Workload cleanup failed: {exception.GetType().Name}; cleanup remains pending.");
            throw;
        }
    }

    public Task<WorkloadEndpoint> Start()
    {
        if (_completion is not null)
        {
            throw new InvalidOperationException("The resource workload has already started.");
        }
        _completion = RunAsync();

        return _ready.Task;
    }

    private async Task RunAsync()
    {
        using var operation = new NativeDiagnostics.Operation("runtime.workload", null, identity.GenerationId);
        operation.SetResource(identity.ResourceId);
        try
        {
            await waitForDependencies(_lifetime.Token).ConfigureAwait(false);
            // Dependency readiness has its own lifetime. Start this workload's
            // startup budget only after dependencies are ready.
            using var startup = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            startup.CancelAfter(options.WorkloadStartupTimeout);
            var lease = await executor.StartAsync(identity, plan, startup.Token).ConfigureAwait(false);
            _lease = lease;
            publish("Running", lease.Endpoint);
            _ready.TrySetResult(lease.Endpoint);
            while (true)
            {
                foreach (var line in await lease.ReadLogsAsync(_lifetime.Token).ConfigureAwait(false))
                {
                    log(line.Stream, line.Message);
                }
                var status = await lease.ReadStatusAsync(_lifetime.Token).ConfigureAwait(false);
                if (!status.Running)
                {
                    publish(status.ExitCode == 0 ? "Stopped" : "Failed", null);
                    break;
                }
                await Task.Delay(options.ObservationInterval, _lifetime.Token).ConfigureAwait(false);
            }
            operation.Succeeded = true;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
            _ready.TrySetCanceled(_lifetime.Token);
            operation.Succeeded = true;
        }
        catch (Exception exception)
        {
            // Record only the classification. Executor failures can carry
            // credentials from tools; callers retain the original exception.
            operation.ErrorType = exception.GetType().FullName;
            if (exception is WorkloadCleanupException cleanupFailure)
            {
                _lease = cleanupFailure.Lease;
                _cleanupFailed = true;
            }
            if (exception is WorkloadStartupException startupFailure)
            {
                foreach (var line in startupFailure.Logs)
                {
                    log(line.Stream, line.Message);
                }
            }
            log("stderr", $"Workload failed: {exception.GetType().Name}.");
            publish("Failed", null);
            _ready.TrySetException(exception);
            throw;
        }
        finally
        {
            try
            {
                if (_lease is not null && !_cleanupFailed)
                {
                    try
                    {
                        await CleanupAsync().ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        operation.Succeeded = false;
                        operation.ErrorType = exception.GetType().FullName;
                        throw;
                    }
                }
            }
            finally
            {
                lock (_lifetimeGate)
                {
                    _finished = true;
                    _lifetime.Dispose();
                }
            }
        }
    }

    public void Dispose()
    {
        lock (_lifetimeGate)
        {
            if (!_finished)
            {
                _lifetime.Cancel();
            }
        }
    }
}
