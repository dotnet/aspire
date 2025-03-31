// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Testing;

internal static class DistributedApplicationEntryPointInvoker
{
    // This helpers encapsulates all of the complex logic required to:
    // 1. Execute the entry point of the specified assembly in a different thread.
    // 2. Wait for the diagnostic source events to fire
    // 3. Give the caller a chance to execute logic to mutate the IDistributedApplicationBuilder
    // 4. Resolve the instance of the DistributedApplication
    // 5. Allow the caller to determine if the entry point has completed
    public static Func<string[], CancellationToken, Task<DistributedApplication>>? ResolveEntryPoint(
        Assembly assembly,
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing = null,
        Action<DistributedApplicationBuilder>? onConstructed = null,
        Action<DistributedApplicationBuilder>? onBuilding = null,
        Action<Exception?>? entryPointCompleted = null)
    {
        if (assembly.EntryPoint is null)
        {
            return null;
        }

        return async (args, ct) =>
        {
            var invoker = new EntryPointInvoker(
                args,
                assembly.EntryPoint,
                onConstructing,
                onConstructed,
                onBuilding,
                entryPointCompleted);
            return await invoker.InvokeAsync(ct).ConfigureAwait(false);
        };
    }

    private sealed class EntryPointInvoker : IObserver<DiagnosticListener>
    {
        private static readonly AsyncLocal<EntryPointInvoker> s_currentListener = new();
        private readonly string[] _args;
        private readonly MethodInfo _entryPoint;
        private readonly TaskCompletionSource<DistributedApplication> _appTcs = new();
        private readonly ApplicationBuilderDiagnosticListener _applicationBuilderListener;
        private readonly Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? _onConstructing;
        private readonly Action<DistributedApplicationBuilder>? _onConstructed;
        private readonly Action<DistributedApplicationBuilder>? _onBuilding;
        private readonly Action<Exception?>? _entryPointCompleted;

        public EntryPointInvoker(
            string[] args,
            MethodInfo entryPoint,
            Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing,
            Action<DistributedApplicationBuilder>? onConstructed,
            Action<DistributedApplicationBuilder>? onBuilding,
            Action<Exception?>? entryPointCompleted)
        {
            _args = args;
            _entryPoint = entryPoint;
            _onConstructing = onConstructing;
            _onConstructed = onConstructed;
            _onBuilding = onBuilding;
            _entryPointCompleted = entryPointCompleted;
            _applicationBuilderListener = new(this);
        }

        public async Task<DistributedApplication> InvokeAsync(CancellationToken cancellationToken)
        {
            using var subscription = DiagnosticListener.AllListeners.Subscribe(this);

            // Kick off the entry point on a new thread so we don't block the current one
            // in case we need to timeout the execution
            var thread = new Thread(() =>
            {
                Exception? exception = null;

                try
                {
                    // Set the async local to the instance of the HostingListener so we can filter events that
                    // aren't scoped to this execution of the entry point.
                    s_currentListener.Value = this;

                    var parameters = _entryPoint.GetParameters();
                    object? result;
                    if (parameters.Length == 0)
                    {
                        result = _entryPoint.Invoke(null, []);
                    }
                    else
                    {
                        result = _entryPoint.Invoke(null, [_args]);
                    }

                    // Try to set an exception if the entry point returns gracefully, this will force
                    // build to throw
                    _appTcs.TrySetException(new InvalidOperationException($"The entry point exited without building a {nameof(DistributedApplication)}."));
                }
                catch (TargetInvocationException tie)
                {
                    exception = tie.InnerException ?? tie;

                    // Another exception happened, propagate that to the caller
                    _appTcs.TrySetException(exception);
                }
                catch (Exception ex)
                {
                    exception = ex;

                    // Another exception happened, propagate that to the caller
                    _appTcs.TrySetException(exception);
                }
                finally
                {
                    // Signal that the entry point is completed
                    _entryPointCompleted?.Invoke(exception);
                }
            })
            {
                // Make sure this doesn't hang the process
                IsBackground = true,
                Name = $"{_entryPoint.DeclaringType?.Assembly.GetName().Name ?? "Unknown"}.EntryPoint"
            };

            // Start the thread
            thread.Start();

            return await _appTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {

        }

        public void OnNext(DiagnosticListener value)
        {
            if (s_currentListener.Value != this)
            {
                // Ignore events that aren't for this listener
                return;
            }

            if (value.Name == "Aspire.Hosting")
            {
                _applicationBuilderListener.Subscribe(value);
            }
        }

        private sealed class ApplicationBuilderDiagnosticListener(EntryPointInvoker owner) : IObserver<KeyValuePair<string, object?>>
        {
            private IDisposable? _disposable;

            public void Subscribe(DiagnosticListener listener)
            {
                _disposable = listener.Subscribe(this);
            }

            public void OnCompleted()
            {
                _disposable?.Dispose();
            }

            public void OnError(Exception error)
            {
            }

            public void OnNext(KeyValuePair<string, object?> value)
            {
                if (s_currentListener.Value != owner)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Key == "DistributedApplicationBuilderConstructing")
                {
                    var args = ((DistributedApplicationOptions Options, HostApplicationBuilderSettings InnerBuilderOptions))value.Value!;
                    owner._onConstructing?.Invoke(args.Options, args.InnerBuilderOptions);
                }

                if (value.Key == "DistributedApplicationBuilderConstructed")
                {
                    owner._onConstructed?.Invoke((DistributedApplicationBuilder)value.Value!);
                }

                if (value.Key == "DistributedApplicationBuilding")
                {
                    owner._onBuilding?.Invoke((DistributedApplicationBuilder)value.Value!);
                }

                if (value.Key == "DistributedApplicationBuilt")
                {
                    owner._appTcs.TrySetResult((DistributedApplication)value.Value!);
                }
            }
        }
    }
}

internal sealed class TestingBuilderFactory : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private static readonly ThreadLocal<TestingBuilderFactory?> s_currentListener = new();
    private readonly Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? _onConstructing;
    private IDisposable? _hostingListener;

    private TestingBuilderFactory(Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing)
    {
        _onConstructing = onConstructing;
    }

    public static DistributedApplicationBuilder CreateBuilder(
        string[] args,
        Action<DistributedApplicationOptions, HostApplicationBuilderSettings>? onConstructing)
    {
        using var observer = new TestingBuilderFactory(onConstructing);
        using var subscription = DiagnosticListener.AllListeners.Subscribe(observer);

        try
        {
            s_currentListener.Value = observer;
            return new DistributedApplicationBuilder(args);
        }
        finally
        {
            s_currentListener.Value = null;
        }
    }

    void IObserver<DiagnosticListener>.OnCompleted()
    {
    }

    void IObserver<DiagnosticListener>.OnError(Exception error)
    {
    }

    void IObserver<DiagnosticListener>.OnNext(DiagnosticListener value)
    {
        if (s_currentListener.Value != this)
        {
            // Ignore events that aren't for this listener
            return;
        }

        if (value.Name == "Aspire.Hosting")
        {
            _hostingListener = value.Subscribe(this);
        }
    }

    void IObserver<KeyValuePair<string, object?>>.OnCompleted()
    {
        _hostingListener?.Dispose();
    }

    void IObserver<KeyValuePair<string, object?>>.OnError(Exception error)
    {
    }

    void IObserver<KeyValuePair<string, object?>>.OnNext(KeyValuePair<string, object?> value)
    {
        if (s_currentListener.Value != this)
        {
            // Ignore events that aren't for this listener
            return;
        }

        if (value.Key == "DistributedApplicationBuilderConstructing")
        {
            var (options, innerBuilderOptions) = ((DistributedApplicationOptions Options, HostApplicationBuilderSettings InnerBuilderOptions))value.Value!;
            _onConstructing?.Invoke(options, innerBuilderOptions);
        }
    }

    public void Dispose()
    {
        _hostingListener?.Dispose();
    }
}
