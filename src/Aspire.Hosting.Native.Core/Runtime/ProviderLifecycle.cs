// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.Native.Runtime;

/// <summary>Identifies an offline-validated provider callback without transport or language objects.</summary>
internal sealed record ProviderLifecycleBinding(Guid ProviderId, string ObjectId, string TypeId,
    string Phase, string CapabilityId);

/// <summary>Separates native lifecycle ordering and authority from the provider transport.</summary>
internal interface IProviderLifecycleDispatcher
{
    Task InvokeAsync(ProviderLifecycleBinding binding, ResourceLifecycleAuthority authority, CancellationToken cancellationToken);
}

internal sealed class UnavailableProviderLifecycleDispatcher : IProviderLifecycleDispatcher
{
    public static UnavailableProviderLifecycleDispatcher Instance { get; } = new();
    public Task InvokeAsync(ProviderLifecycleBinding binding, ResourceLifecycleAuthority authority, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No provider lifecycle dispatcher is configured.");
}

/// <summary>Grants one phase native execution with captured inputs, never credential inspection or declaration access.</summary>
internal sealed class ResourceLifecycleAuthority(string phase, ImmutableDictionary<string, string> values,
    Func<WorkloadPlan, CancellationToken, Task<bool>> execute, Action<LogStream, string> log,
    CancellationToken cancellationToken) : IDisposable
{
    private readonly CancellationTokenSource _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    private readonly Lock _gate = new();
    private readonly List<Task<bool>> _executions = [];
    public string Phase { get; } = phase;
    public bool IsRevoked => _lifetime.IsCancellationRequested;
    public void AppendLog(LogStream stream, string message)
    {
        _lifetime.Token.ThrowIfCancellationRequested();
        log(stream, message);
    }

    public Task<bool> RunExecutable(ExecutableWorkload template, SymbolicWorkloadBindings bindings)
    {
        _lifetime.Token.ThrowIfCancellationRequested();
        bindings.Validate(template);
        string Resolve(string key) => values.TryGetValue(key, out var value) ? value :
            throw new InvalidOperationException("The lifecycle field is unavailable in this phase.");
        var environment = template.Environment.AddRange(bindings.Environment.Select(binding =>
            new WorkloadEnvironment(binding.Name, Resolve(binding.ConfigurationKey))));
        var arguments = template.Arguments.ToBuilder();
        foreach (var binding in bindings.Arguments)
        {
            arguments[binding.Index] = Resolve(binding.ConfigurationKey);
        }
        var plan = SymbolicWorkloadBindings.Copy(template, environment, arguments.ToImmutable());
        WorkloadDefinition.Validate(plan);

        lock (_gate)
        {
            _lifetime.Token.ThrowIfCancellationRequested();
            var execution = execute(plan, _lifetime.Token);
            _executions.Add(execution);
            _ = execution.ContinueWith(static completed => _ = completed.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

            return execution;
        }
    }

    public async Task DrainAsync()
    {
        Task<bool>[] executions;
        lock (_gate)
        {
            executions = _executions.ToArray();
        }
        try
        {
            await Task.WhenAll(executions).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (IsRevoked)
        {
            // Cancellation revokes the phase, but all native execution cleanup
            // must finish before its provider-local object can be disposed.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _lifetime.Cancel();
        }
    }
}
