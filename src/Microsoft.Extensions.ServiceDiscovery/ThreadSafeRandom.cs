// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery;

internal static class ThreadSafeRandom
{
#if NET6_0_OR_GREATER
    public static int Next(int maxValue)
    {
        return Random.Shared.Next(maxValue);
    }
#else
    private static readonly Random s_random = new();

    public static int Next(int maxValue)
    {
        lock (s_random)
        {
            return s_random.Next(maxValue);
        }
    }
#endif
}
