// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Model;

public enum OtlpSpanStatusCode
{
    /// <summary>
    /// The default status.
    /// </summary>
    Unset = 0,
    /// <summary>
    /// The Span has been validated by an Application developer or Operator to 
    /// have completed successfully.
    /// </summary>
    Ok = 1,
    /// <summary>
    /// The Span contains an error.
    /// </summary>
    Error = 2,
}
