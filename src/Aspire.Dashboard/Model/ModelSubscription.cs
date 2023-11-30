// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public sealed class ModelSubscription(Func<Task> callback, Action<ModelSubscription> onDispose) : IDisposable
{
    private readonly Func<Task> _callback = callback;
    private readonly Action<ModelSubscription> _onDispose = onDispose;

    public void Dispose() => _onDispose(this);
    public Task ExecuteAsync() => _callback();
}
