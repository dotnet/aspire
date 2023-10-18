// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;

namespace Aspire.Dashboard.ConsoleLogs;

internal readonly record struct LogParserOptions(LogEntryType LogEntryType = LogEntryType.Default, bool ConvertTimestampsFromUtc = false);
