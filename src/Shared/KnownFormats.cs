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
    /// UI timestamp displayed on the console logs UI.
    /// </summary>
    public const string ConsoleLogsUITimestampFormat = "yyyy-MM-ddTHH:mm:ss";
}
