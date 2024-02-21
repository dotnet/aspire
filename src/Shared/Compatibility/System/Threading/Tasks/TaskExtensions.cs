// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

internal static class TaskExtensions
{
    public static ConfiguredTaskAwaitable SuppressThrowing(this Task task)
    {
#if NET8_0_OR_GREATER
        return task.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
        return Task.Run(async () =>
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // suppress exception when Task throws
            }
        }).ConfigureAwait(false);
#endif
    }
}
