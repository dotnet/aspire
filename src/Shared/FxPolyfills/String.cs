// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal static partial class FxPolyfillString
{
    extension(string s)
    {
        public bool StartsWith(char c) => s is [{ } first, ..] && first == c;
    }
}
