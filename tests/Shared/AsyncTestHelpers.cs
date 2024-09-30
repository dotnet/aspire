// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.InternalTesting;

internal static class AsyncTestHelpers
{
    public static Task AssertIsTrueRetryAsync(Func<bool> assert, string message, ILogger? logger = null)
    {
        return AssertIsTrueRetryAsync(() => Task.FromResult(assert()), message, logger);
    }

    public static async Task AssertIsTrueRetryAsync(Func<Task<bool>> assert, string message, ILogger? logger = null)
    {
        const int Retries = 10;

        logger?.LogInformation("Start: " + message);

        for (var i = 0; i < Retries; i++)
        {
            if (i > 0)
            {
                await Task.Delay((i + 1) * (i + 1) * 10);
            }

            if (await assert())
            {
                logger?.LogInformation("End: " + message);
                return;
            }
        }

        throw new InvalidOperationException($"Assert failed after {Retries} retries: {message}");
    }
}
