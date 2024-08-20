// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Aspire.Dashboard.Otlp.Model;
using Xunit;
using static Aspire.Dashboard.Components.Pages.Metrics;

namespace Aspire.Dashboard.Components.Tests.Pages;

public sealed class MetricsViewModelTests
{
    [Fact]
    public void IsDashpageAvailable()
    {
        OtlpInstrument instrument = new()
        {
            Name = "present-instrument",
            Description = "",
            Options = null!,
            Parent = null!,
            Type = OtlpInstrumentType.Histogram,
            Unit = null!
        };

        MetricsViewModel vm = new()
        {
            Dashpages = [],
            SelectedDuration = null!,
            SelectedViewKind = MetricViewKind.Graph,
            SelectedApplication = new() { Id = ResourceTypeDetails.CreateSingleton("my-instance", "my-replica-set"), Name = "" },
            Instruments = [instrument],
            ApplicationNames = ["present-application"],
        };

        Assert.True(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "present-instrument", Title = "" }] }));
        Assert.True(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "present-instrument", Title = "", ResourceName = "present-application" }] }));
        Assert.False(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "present-instrument", Title = "", ResourceName = "absent-application" }] }));
        Assert.True(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "present-instrument", Title = "" }, new() { InstrumentName = "absent-instrument", Title = "" }] }));
        Assert.False(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "absent-instrument", Title = "" }] }));
        Assert.False(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "absent-instrument", Title = "" }, new() { InstrumentName = "absent-instrument", Title = "" }] }));
        Assert.False(vm.IsDashpageAvailable(new() { Name = "", Charts = [new() { InstrumentName = "present-instrument", Title = "" }, new() { InstrumentName = "absent-instrument", Title = "", IsRequired = true }] }));
    }
}
