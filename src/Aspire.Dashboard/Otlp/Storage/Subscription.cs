// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Dashboard.Utils;

namespace Aspire.Dashboard.Otlp.Storage;

[DebuggerDisplay("Name = {Name}, ApplicationKey = {ApplicationKey}, SubscriptionId = {SubscriptionId}")]
public sealed class Subscription : IDisposable
{
    private static int s_subscriptionId;

    private readonly CallbackThrottler _callbackThrottler;
    private readonly Action _unsubscribe;
    private readonly int _subscriptionId = Interlocked.Increment(ref s_subscriptionId);

    public int SubscriptionId => _subscriptionId;
    public ApplicationKey? ApplicationKey { get; }
    public SubscriptionType SubscriptionType { get; }
    public string Name { get; }

    public Subscription(string name, ApplicationKey? applicationKey, SubscriptionType subscriptionType, Func<Task> callback, Action unsubscribe, ExecutionContext? executionContext, TelemetryRepository telemetryRepository)
    {
        Name = name;
        ApplicationKey = applicationKey;
        SubscriptionType = subscriptionType;
        _callbackThrottler = new CallbackThrottler(name, telemetryRepository._otlpContext.Logger, telemetryRepository._subscriptionMinExecuteInterval, callback, executionContext);
        _unsubscribe = unsubscribe;
    }

    public void Execute()
    {
        _callbackThrottler.Execute();
    }

    public void Dispose()
    {
        _unsubscribe();
        _callbackThrottler.Dispose();
    }
}
