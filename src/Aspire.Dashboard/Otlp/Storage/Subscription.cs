// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class Subscription : IDisposable
{
    private readonly Func<Task> _callback;
    private readonly ExecutionContext? _executionContext;
    private readonly Action _unsubscribe;

    public string? ApplicationId { get; }
    public SubscriptionType SubscriptionType { get; }

    public Subscription(string? applicationId, SubscriptionType subscriptionType, Func<Task> callback, Action unsubscribe, ExecutionContext? executionContext)
    {
        ApplicationId = applicationId;
        SubscriptionType = subscriptionType;
        _callback = callback;
        _unsubscribe = unsubscribe;
        _executionContext = executionContext;
    }

    public async Task ExecuteAsync()
    {
        if (_executionContext != null)
        {
            var current = ExecutionContext.Capture();
            try
            {
                ExecutionContext.Restore(_executionContext);
                await _callback().ConfigureAwait(false);
            }
            finally
            {
                if (current != null)
                {
                    ExecutionContext.Restore(current);
                }
            }
        }
        else
        {
            await _callback().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _unsubscribe();
    }
}
