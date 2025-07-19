// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

namespace System;

internal static class FrameworkExtensions
{
    extension(OperatingSystem)
    {
        public static bool IsLinux() => false;
        public static bool IsWindows() => true;
        public static bool IsMacOS() => false;
    }
}

#endif
