﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;

namespace Aspire.Dashboard.Components.Tests.Controls;

public sealed class TestInstrumentUnitResolver : IInstrumentUnitResolver
{
    public string ResolveDisplayedUnit(OtlpInstrument instrument, bool titleCase, bool pluralize)
    {
        return instrument.Unit;
    }
}
