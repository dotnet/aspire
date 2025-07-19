// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

namespace System.Threading.Tasks;

internal enum ConfigureAwaitOptions
{
    None,
    ContinueOnCapturedContext,
    ForceYielding,
    SuppressThrowing,
}

internal static partial class FxPolyfillTask
{
    extension(Task task)
    {
        public async Task ConfigureAwait(ConfigureAwaitOptions options)
        {
            if (options == ConfigureAwaitOptions.None)
            {
                await task.ConfigureAwait(false);
            }
            else if (options == ConfigureAwaitOptions.ContinueOnCapturedContext)
            {
                await task.ConfigureAwait(true);
            }
            else if (options == ConfigureAwaitOptions.ForceYielding)
            {
                await Task.Yield();
                await task.ConfigureAwait(false);
            }
            else if (options == ConfigureAwaitOptions.SuppressThrowing)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch
                {
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}

internal sealed class TaskCompletionSource(TaskCreationOptions options) : TaskCompletionSource<bool>(options)
{
    public void SetResult() => SetResult(true);
}

#endif
