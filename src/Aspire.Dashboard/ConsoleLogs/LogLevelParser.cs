// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Aspire.Dashboard.ConsoleLogs;

public static partial class LogLevelParser
{
    private static readonly Regex s_logLevelRegEx = GenerateLogLevelRegEx();

    public static bool StartsWithLogLevelHeader(string text)
     => s_logLevelRegEx.IsMatch(text);

    // Regular expression that detects log levels used as indicators
    // of the first line of a log entry, skipping any ANSI control sequences
    // that may come first
    //
    // Explanation:
    // ^                                 - Starts the string
    // (?:\x1B\\[\\d{1,2}m)*             - Zero or more ANSI SGR Control Sequences (e.g. \x1B[32m, which means green foreground)
    // (?:trce|dbug|info|warn|fail|crit) - One of the log level names
    // :                                 - Literal
    // $                                 - Ends the string
    [GeneratedRegex("^(?:\x1B\\[\\d{1,2}m)*(?:trce|dbug|info|warn|fail|crit)(?:\u001b\\[\\d{1,2}m)*:")]
    private static partial Regex GenerateLogLevelRegEx();
}
