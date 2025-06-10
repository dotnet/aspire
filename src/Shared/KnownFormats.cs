// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

internal static class KnownFormats
{
    /// <summary>
    /// Internal timestamp format that is used to add the timestamp to a log line.
    /// Preserve second precision and timezone information.
    /// </summary>
    public const string ConsoleLogsTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffffffK";

    /// <summary>
    /// UI timestamp displayed on the console logs UI for local timestamps.
    /// </summary>
    public const string ConsoleLogsUITimestampLocalFormat = "yyyy-MM-ddTHH:mm:ss";

    /// <summary>
    /// UI timestamp displayed on the console logs UI for UTC timestamps.
    /// </summary>
    public const string ConsoleLogsUITimestampUtcFormat = "yyyy-MM-ddTHH:mm:ssK";
}
