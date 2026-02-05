// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
internal interface IConsole
{
    event Action<ConsoleKeyInfo> KeyPressed;
    TextWriter Out { get; }
    TextWriter Error { get; }
    ConsoleColor ForegroundColor { get; set; }
    void ResetColor();
    void Clear();
}
