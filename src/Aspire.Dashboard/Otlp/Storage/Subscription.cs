// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class Subscription : IDisposable
{
    private readonly Func<Task> _callback;
    private readonly ExecutionContext? _executionContext;
    private readonly ILogger _logger;
    private readonly Action _unsubscribe;

    public string? ApplicationId { get; }
    public SubscriptionType SubscriptionType { get; }

    public Subscription(string? applicationId, SubscriptionType subscriptionType, Func<Task> callback, Action unsubscribe, ExecutionContext? executionContext, ILogger logger)
    {
        ApplicationId = applicationId;
        SubscriptionType = subscriptionType;
        _callback = callback;
        _unsubscribe = unsubscribe;
        _executionContext = executionContext;
        _logger = logger;
    }

    public void Execute()
    {
        // Execute the subscription callback on a background thread.
        // The caller doesn't want to wait while the subscription is running or receive exceptions.
        _ = Task.Run(async () =>
        {
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

                await _callback().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription callback");
            }
        });
    }

    public void Dispose()
    {
        _unsubscribe();
    }
}
