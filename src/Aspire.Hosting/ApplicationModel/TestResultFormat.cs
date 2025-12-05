// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies the format of test result files.
/// </summary>
public enum TestResultFormat
{
    /// <summary>
    /// The pytest report-log format (JSON lines format with one JSON object per line).
    /// </summary>
    PytestReportLog
}
