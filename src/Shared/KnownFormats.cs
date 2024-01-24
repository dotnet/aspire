// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire;

internal static class KnownFormats
{
    /// <summary>
    /// Format is passed to apps as an env var to override logging's timestamp format.
    /// It is also used to parse logs when they're displayed in the dashboard's console logs UI.
    /// </summary>
    public const string ConsoleLogsTimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";
}
