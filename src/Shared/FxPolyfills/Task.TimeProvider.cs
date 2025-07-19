// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

namespace System.Threading.Tasks;

internal static partial class FxPolyfillTask
{
    extension(Task task)
    {
        public Task WaitAsync(CancellationToken token)
        {
            return task.WaitAsync(Timeout.InfiniteTimeSpan, TimeProvider.System, token);
        }
    }
}

#endif
