// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal static class BuildUtilities
{
    // Parses name=value pairs passed to --property. Skips invalid input.
    public static IEnumerable<(string key, string value)> ParseBuildProperties(IEnumerable<string> arguments)
        => from argument in arguments
           let colon = argument.IndexOf(':')
           where colon >= 0 && argument[0..colon] is "--property" or "-property" or "/property" or "/p" or "-p" or "--p"
           let eq = argument.IndexOf('=', colon)
           where eq >= 0
           let name = argument[(colon + 1)..eq].Trim()
           let value = argument[(eq + 1)..]
           where name is not []
           select (name, value);
}
