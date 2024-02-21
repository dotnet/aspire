// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading;

internal static class InterlockedEx
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Add(ref uint location1, uint value) =>
        (uint)Interlocked.Add(ref Unsafe.As<uint, int>(ref location1), (int)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Add(ref ulong location1, ulong value) =>
        (ulong)Interlocked.Add(ref Unsafe.As<ulong, long>(ref location1), (long)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Increment(ref uint location) =>
        Add(ref location, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Increment(ref ulong location) =>
        Add(ref location, 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Decrement(ref ulong location) =>
        (ulong)Interlocked.Add(ref Unsafe.As<ulong, long>(ref location), -1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Or(ref long location1, long value)
    {
        long current = location1;
        while (true)
        {
            long newValue = current | value;
            long oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
            if (oldValue == current)
            {
                return oldValue;
            }
            current = oldValue;
        }
    }

    public static ulong Or(ref ulong location1, ulong value) =>
        (ulong)Or(ref Unsafe.As<ulong, long>(ref location1), (long)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long And(ref long location1, long value)
    {
        long current = location1;
        while (true)
        {
            long newValue = current & value;
            long oldValue = Interlocked.CompareExchange(ref location1, newValue, current);
            if (oldValue == current)
            {
                return oldValue;
            }
            current = oldValue;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong And(ref ulong location1, ulong value) =>
        (ulong)And(ref Unsafe.As<ulong, long>(ref location1), (long)value);
}
