// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETFRAMEWORK

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.ExceptionServices;

internal static partial class FxPolyfillExceptionDispatchInfo
{
    extension(ExceptionDispatchInfo)
    {
        [DoesNotReturn]
        public static void Throw(Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }
}

#endif
