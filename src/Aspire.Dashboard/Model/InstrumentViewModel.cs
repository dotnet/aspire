// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Otlp.Model.MetricValues;

namespace Aspire.Dashboard.Model;

public class InstrumentViewModel
{
    public OtlpInstrument? Instrument { get; private set; }
    public List<DimensionScope>? MatchedDimensions { get; private set; }

    public Func<Task>? OnDataUpdate { get; set; }

    public async Task UpdateDataAsync(OtlpInstrument instrument, List<DimensionScope> matchedDimensions)
    {
        Instrument = instrument;
        MatchedDimensions = matchedDimensions;
        if (OnDataUpdate is not null)
        {
            await OnDataUpdate().ConfigureAwait(false);
        }
    }
}
