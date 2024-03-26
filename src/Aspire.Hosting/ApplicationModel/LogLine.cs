// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a console log line.
/// </summary>
/// <param name="LineNumber">The line number.</param>
/// <param name="Content">The content.</param>
/// <param name="IsErrorMessage">A value indicating whether the log line is error output.</param>
public readonly record struct LogLine(int LineNumber, string Content, bool IsErrorMessage)
{
}
