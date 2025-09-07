// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Dashboard.Otlp.Model;

public static class DurationFormatter
{
    [DebuggerDisplay("Unit = {Unit}, Ticks = {Ticks}, IsDecimal = {IsDecimal}")]
    private sealed class UnitStep
    {
        public required string Unit { get; init; }
        public required long Ticks { get; init; }
        public required long Threshold { get; init; }
        public bool IsDecimal { get; init; }
    }

    private static readonly List<UnitStep> s_unitSteps = new List<UnitStep>
    {
        new UnitStep { Unit = "d", Ticks = TimeSpan.TicksPerDay, Threshold = TimeSpan.TicksPerDay },
        new UnitStep { Unit = "h", Ticks = TimeSpan.TicksPerHour, Threshold = TimeSpan.TicksPerHour },
        new UnitStep { Unit = "m", Ticks = TimeSpan.TicksPerMinute, Threshold = TimeSpan.TicksPerMinute },
        new UnitStep { Unit = "s", Ticks = TimeSpan.TicksPerSecond, Threshold = TimeSpan.TicksPerSecond / 10, IsDecimal = true },
        new UnitStep { Unit = "ms", Ticks = TimeSpan.TicksPerMillisecond, Threshold = TimeSpan.TicksPerMillisecond / 100, IsDecimal = true },
        new UnitStep { Unit = "μs", Ticks = TimeSpan.TicksPerMicrosecond, Threshold = TimeSpan.TicksPerMicrosecond, IsDecimal = true },
    };

    public static string FormatDuration(TimeSpan duration)
    {
        var (primaryUnit, secondaryUnit) = ResolveUnits(duration.Ticks);
        var ofPrevious = primaryUnit.Ticks / secondaryUnit.Ticks;
        var ticks = (double)duration.Ticks;

        if (primaryUnit.IsDecimal)
        {
            // If the unit is decimal based, display as a decimal
            return $"{ticks / primaryUnit.Ticks:0.##}{primaryUnit.Unit}";
        }

        var primaryValue = Math.Floor(ticks / primaryUnit.Ticks);
        var primaryUnitString = $"{primaryValue}{primaryUnit.Unit}";
        var secondaryValue = Math.Round((ticks / secondaryUnit.Ticks) % ofPrevious, MidpointRounding.AwayFromZero);
        var secondaryUnitString = $"{secondaryValue}{secondaryUnit.Unit}";

        return secondaryValue == 0 ? primaryUnitString : $"{primaryUnitString} {secondaryUnitString}";
    }

    public static string GetUnit(TimeSpan duration)
    {
        var (primaryUnit, secondaryUnit) = ResolveUnits(duration.Ticks);
        if (primaryUnit.IsDecimal)
        {
            return primaryUnit.Unit;
        }
        return secondaryUnit.Unit;
    }

    private static (UnitStep, UnitStep) ResolveUnits(long ticks)
    {
        for (var i = 0; i < s_unitSteps.Count; i++)
        {
            var step = s_unitSteps[i];
            var result = i < s_unitSteps.Count - 1 && step.Threshold > ticks;

            if (!result)
            {
                return (step, i < s_unitSteps.Count - 1 ? s_unitSteps[i + 1] : step);
            }
        }

        return (s_unitSteps[^1], s_unitSteps[^1]);
    }
}
