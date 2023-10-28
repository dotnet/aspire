// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Storage;

public sealed class Subscription : IDisposable
{
    private readonly Func<Task> _callback;
    private readonly Action _unsubscribe;

    public string? ApplicationId { get; }
    public SubscriptionType SubscriptionType { get; }

    public Subscription(string? applicationId, SubscriptionType subscriptionType, Func<Task> callback, Action unsubscribe)
    {
        ApplicationId = applicationId;
        SubscriptionType = subscriptionType;
        _callback = callback;
        _unsubscribe = unsubscribe;
    }

    public Task ExecuteAsync() => _callback();

    public void Dispose()
    {
        _unsubscribe();
    }
}
