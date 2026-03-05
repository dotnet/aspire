// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using System.CommandLine.Parsing;

namespace Microsoft.DotNet.Watch;

internal static class OptionExtensions
{
    public static bool HasOption(this SymbolResult symbolResult, Option option)
        => symbolResult.GetResult(option) is OptionResult or && !or.Implicit;
}
