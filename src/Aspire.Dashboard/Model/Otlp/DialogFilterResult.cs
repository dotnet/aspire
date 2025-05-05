// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model.Otlp;

public class FilterDialogResult
{
    public TelemetryFilter? Filter { get; set; }
    public bool Delete { get; set; }
    public bool Add { get; set; }
    public bool Disable { get; set; }
    public bool Enable { get; set; }
}
