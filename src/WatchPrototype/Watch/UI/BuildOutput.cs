// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal static partial class BuildOutput
{
    private static readonly Regex s_buildDiagnosticRegex = GetBuildDiagnosticRegex();

    [GeneratedRegex(@"[^:]+: (error|warning) [A-Za-z]+[0-9]+: .+")]
    private static partial Regex GetBuildDiagnosticRegex();

    public static void ReportBuildOutput(ILogger logger, IEnumerable<OutputLine> buildOutput, bool success)
    {
        foreach (var (line, isError) in buildOutput)
        {
            if (isError)
            {
                logger.LogError(line);
            }
            else if (s_buildDiagnosticRegex.Match(line) is { Success: true } match)
            {
                if (match.Groups[1].Value == "error")
                {
                    logger.LogError(line);
                }
                else
                {
                    logger.LogWarning(line);
                }
            }
            else if (success)
            {
                logger.LogDebug(line);
            }
            else
            {
                logger.LogInformation(line);
            }
        }
    }
}
