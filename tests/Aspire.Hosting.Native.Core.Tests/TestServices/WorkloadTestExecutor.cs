// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Aspire.Hosting.Native.Runtime;

namespace Aspire.Hosting.Native.Core.Tests.TestServices;

/// <summary>Tracks execution and gates cleanup without substituting the native model or runtime services.</summary>
internal sealed class WorkloadTestExecutor : IWorkloadExecutor
{
    private readonly TaskCompletionSource _cleanup = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public ConcurrentQueue<WorkloadIdentity> Started { get; } = new();
    public ConcurrentQueue<WorkloadPlan> Plans { get; } = new();
    public ConcurrentQueue<WorkloadIdentity> Removed { get; } = new();
    public TaskCompletionSource CleanupStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Exception? StartFailure { get; init; }
    public Func<CancellationToken, Task>? BeforeStart { get; init; }
    public WorkloadPhase Phase { get; set; } = WorkloadPhase.Running;
    public int? ExitCode { get; set; }

    public void ReleaseCleanup() => _cleanup.TrySetResult();
    public void FailCleanup() => _cleanup.TrySetException(new InvalidOperationException("Test workload cleanup failed."));

    public async Task<IWorkloadLease> StartAsync(WorkloadIdentity identity, WorkloadPlan plan, CancellationToken cancellationToken)
    {
        if (BeforeStart is { } before)
        {
            await before(cancellationToken).ConfigureAwait(false);
        }
        cancellationToken.ThrowIfCancellationRequested();
        Started.Enqueue(identity);
        Plans.Enqueue(plan);
        if (StartFailure is not null)
        {
            throw StartFailure;
        }

        return new Lease(this, identity, plan);
    }

    private sealed class Lease(WorkloadTestExecutor executor, WorkloadIdentity identity, WorkloadPlan plan) : IWorkloadLease
    {
        public WorkloadEndpoint Endpoint { get; } = new("test-instance",
            plan.Endpoints.Select((endpoint, index) => new AllocatedWorkloadPort(
                endpoint.Name, endpoint.Protocol, endpoint.UriScheme, "127.0.0.1", 5050 + index)).ToImmutableArray());
        public Task<WorkloadStatus> ReadStatusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new WorkloadStatus(executor.Phase, executor.ExitCode));
        public Task<ImmutableArray<WorkloadLog>> ReadLogsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(ImmutableArray<WorkloadLog>.Empty);
        public async ValueTask DisposeAsync()
        {
            executor.CleanupStarted.TrySetResult();
            await executor._cleanup.Task.ConfigureAwait(false);
            executor.Removed.Enqueue(identity);
        }
    }
}
