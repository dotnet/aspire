﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Otlp.Model;
using Aspire.Dashboard.Resources;
using Humanizer;
using Microsoft.Extensions.Localization;

namespace Aspire.Dashboard.Model;

public sealed class DefaultInstrumentUnitResolver(IStringLocalizer<ControlsStrings> loc) : IInstrumentUnitResolver
{
    public string ResolveDisplayedUnit(OtlpInstrument instrument)
    {
        if (!string.IsNullOrEmpty(instrument.Unit))
        {
            var unit = OtlpUnits.GetUnit(instrument.Unit.TrimStart('{').TrimEnd('}'));
            return unit.Pluralize().Titleize();
        }

        // Hard code for instrument names that don't have units
        // but have a descriptive name that lets us infer the unit.
        if (instrument.Name.EndsWith(".count"))
        {
            return loc[nameof(ControlsStrings.PlotlyChartCount)];
        }
        else if (instrument.Name.EndsWith(".length"))
        {
            return loc[nameof(ControlsStrings.PlotlyChartLength)];
        }
        else
        {
            return loc[nameof(ControlsStrings.PlotlyChartValue)];
        }
    }
}
