// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Pipelines;

/// <summary>
/// Options for controlling logging behavior in the pipeline.
/// </summary>
internal sealed class PipelineLoggingOptions
{
    /// <summary>
    /// Gets or sets the minimum log level for pipeline logging.
    /// </summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets a value indicating whether exception details (stack traces) should be included in logs.
    /// </summary>
    /// <remarks>
    /// Exception details are included when the minimum log level is Debug or Trace.
    /// </remarks>
    public bool IncludeExceptionDetails => MinimumLogLevel <= LogLevel.Debug;
}
