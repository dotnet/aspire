// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dcp;

/// <summary>
/// Helper class for parsing DCP-formatted log lines.
/// DCP log format: &lt;date&gt;\t&lt;level&gt;\t&lt;category&gt;\t&lt;log message&gt;
/// </summary>
internal static class DcpLogParser
{
    /// <summary>
    /// Tries to parse a DCP-formatted log line.
    /// </summary>
    /// <param name="line">The log line to parse (as bytes).</param>
    /// <param name="message">The extracted message.</param>
    /// <param name="logLevel">The extracted log level.</param>
    /// <param name="category">The extracted category.</param>
    /// <returns>True if the line was successfully parsed as a DCP log; false otherwise.</returns>
    public static bool TryParseDcpLog(ReadOnlySpan<byte> line, out string message, out LogLevel logLevel, out string category)
    {
        message = string.Empty;
        logLevel = LogLevel.Information;
        category = string.Empty;

        try
        {
            // The log format is
            // <date>\t<level>\t<category>\t<log message>
            // e.g. 2023-09-19T20:40:50.509-0700      info    dcpctrl.ServiceReconciler       service /apigateway is now in state Ready       {"ServiceName": {"name":"apigateway"}}

            var tab = line.IndexOf((byte)'\t');
            if (tab < 0)
            {
                return false;
            }

            // Skip date
            line = line[(tab + 1)..];
            
            tab = line.IndexOf((byte)'\t');
            if (tab < 0)
            {
                return false;
            }
            
            var level = line[..tab];
            line = line[(tab + 1)..];
            
            tab = line.IndexOf((byte)'\t');
            if (tab < 0)
            {
                return false;
            }
            
            var categorySpan = line[..tab];
            line = line[(tab + 1)..];

            // Trim trailing carriage return.
            if (line.Length > 0 && line[^1] == '\r')
            {
                line = line[0..^1];
            }

            var messageSpan = line;

            // Parse log level
            if (level.SequenceEqual("info"u8))
            {
                logLevel = LogLevel.Information;
            }
            else if (level.SequenceEqual("error"u8))
            {
                logLevel = LogLevel.Error;
            }
            else if (level.SequenceEqual("warning"u8))
            {
                logLevel = LogLevel.Warning;
            }
            else if (level.SequenceEqual("debug"u8))
            {
                logLevel = LogLevel.Debug;
            }
            else if (level.SequenceEqual("trace"u8))
            {
                logLevel = LogLevel.Trace;
            }

            message = Encoding.UTF8.GetString(messageSpan);
            category = Encoding.UTF8.GetString(categorySpan);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a DCP-formatted log line from a string.
    /// </summary>
    /// <param name="line">The log line to parse (as string).</param>
    /// <param name="message">The extracted message.</param>
    /// <param name="logLevel">The extracted log level.</param>
    /// <param name="isErrorLevel">True if the log level indicates an error.</param>
    /// <returns>True if the line was successfully parsed as a DCP log; false otherwise.</returns>
    public static bool TryParseDcpLog(string line, out string message, out LogLevel logLevel, out bool isErrorLevel)
    {
        var bytes = Encoding.UTF8.GetBytes(line);
        var result = TryParseDcpLog(bytes.AsSpan(), out message, out logLevel, out _);
        isErrorLevel = logLevel == LogLevel.Error;
        return result;
    }
}
