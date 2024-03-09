// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Xunit.Sdk;

namespace Aspire.Workload.Tests;

// taken from https://github.com/dotnet/arcade/blob/main/src/Common/Microsoft.Arcade.Common/CommandResult.cs
public struct CommandResult
{
    public static readonly CommandResult Empty;

    public ProcessStartInfo StartInfo { get; }
    public int ExitCode { get; }
    public string Output { get; }

    public CommandResult(ProcessStartInfo startInfo, int exitCode, string output)
    {
        StartInfo = startInfo;
        ExitCode = exitCode;
        Output = output;
    }

    public CommandResult EnsureSuccessful(string messagePrefix = "", bool suppressOutput = false)
        => EnsureExitCode(0, messagePrefix, suppressOutput);

    public CommandResult EnsureExitCode(int expectedExitCode = 0, string messagePrefix = "", bool suppressOutput = false)
    {
        if (ExitCode != expectedExitCode)
        {
            StringBuilder message = new StringBuilder($"{messagePrefix} Expected {expectedExitCode} exit code but got {ExitCode}: {StartInfo.FileName} {StartInfo.Arguments}");

            if (!suppressOutput)
            {
                if (!string.IsNullOrEmpty(Output))
                {
                    _ = message.AppendLine(CultureInfo.InvariantCulture, $"{Environment.NewLine}Standard Output:{Environment.NewLine}{Output}");
                }
            }

            throw new XunitException(message.ToString());
        }

        return this;
    }
}
