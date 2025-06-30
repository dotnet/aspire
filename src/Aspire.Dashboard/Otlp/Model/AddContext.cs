// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Otlp.Model;

public sealed class AddContext
{
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
}
