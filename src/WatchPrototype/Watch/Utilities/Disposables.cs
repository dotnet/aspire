// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal readonly record struct Disposables(List<object> disposables) : IAsyncDisposable
{
    public List<object> Items => disposables;

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in disposables)
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                ((IDisposable)disposable).Dispose();
            }
        }
    }
}
