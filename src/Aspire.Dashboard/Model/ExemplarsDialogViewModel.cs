// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls.Chart;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Model;

public sealed class ExemplarsDialogViewModel
{
    public required List<ChartExemplar> Exemplars { get; init; }
    public required List<OtlpApplication> Applications { get; init; }
    public required OtlpInstrument Instrument { get; init; }
}
