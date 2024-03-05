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
        // Set the execution context to the one captured when the subscription was created.
        // This ensures that the callback runs in the same context as the subscription was created.
        // For example, the request culture is used to format content in the callback.

        var current = ExecutionContext.Capture();
        try
        {
            if (_executionContext != null)
            {
                ExecutionContext.Restore(_executionContext);
            }

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

    public void Dispose()
    {
        _unsubscribe();
    }
}
