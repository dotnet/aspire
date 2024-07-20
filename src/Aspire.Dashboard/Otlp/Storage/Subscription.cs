// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class Subscription : IDisposable
{
    private readonly Func<Task> _callback;
    private readonly ExecutionContext? _executionContext;
    private readonly TelemetryRepository _telemetryRepository;
    private readonly CancellationTokenSource _cts;
    private readonly CancellationToken _cancellationToken;
    private readonly Action _unsubscribe;
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
    private ILogger Logger => _telemetryRepository._logger;

    private DateTime? _lastExecute;

    public ApplicationKey? ApplicationKey { get; }
    public SubscriptionType SubscriptionType { get; }
    public string Name { get; }

    public Subscription(string name, ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback, Action unsubscribe, ExecutionContext? executionContext, TelemetryRepository telemetryRepository)
    {
        Name = name;
        ApplicationKey = applicationKey;
        SubscriptionType = subscriptionType;
        _callback = callback;
        _unsubscribe = unsubscribe;
        _executionContext = executionContext;
        _telemetryRepository = telemetryRepository;
        _cts = new CancellationTokenSource();
        _cancellationToken = _cts.Token;
    }

    private async Task<bool> TryQueueAsync(CancellationToken cancellationToken)
    {
        var success = _lock.Wait(0, cancellationToken);
        if (!success)
        {
            Logger.LogDebug("Subscription '{Name}' update already queued.", Name);
            return false;
        }

        try
        {
            var lastExecute = _lastExecute;
            if (lastExecute != null)
            {
                var s = lastExecute.Value.Add(_telemetryRepository._subscriptionMinExecuteInterval) - DateTime.UtcNow;
                if (s > TimeSpan.Zero)
                {
                    Logger.LogTrace("Subscription '{Name}' minimum execute interval hit. Waiting {DelayInterval}.", Name, s);
                    await Task.Delay(s, cancellationToken).ConfigureAwait(false);
                }
            }

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Execute()
    {
        // Execute the subscription callback on a background thread.
        // The caller doesn't want to wait while the subscription is running or receive exceptions.
        _ = Task.Run(async () =>
        {
            // Try to queue the subscription callback.
            // If another caller is already in the queue then exit without calling the callback.
            if (!await TryQueueAsync(_cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            try
            {
                // Set the execution context to the one captured when the subscription was created.
                // This ensures that the callback runs in the same context as the subscription was created.
                // For example, the request culture is used to format content in the callback.
                //
                // No need to restore back to the original context because the callback is running on
                // a background task. The task finishes immediately after the callback.
                if (_executionContext != null)
                {
                    ExecutionContext.Restore(_executionContext);
                }

                Logger.LogTrace("Subscription '{Name}' executing.", Name);
                await _callback().ConfigureAwait(false);
                _lastExecute = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in subscription callback");
            }
        });
    }

    public void Dispose()
    {
        _unsubscribe();
        _cts.Cancel();
        _cts.Dispose();
        _lock.Dispose();
    }
}
