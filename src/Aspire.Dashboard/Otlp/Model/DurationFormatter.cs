// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Aspire.Dashboard.Otlp.Model;

/// <summary>
/// Type alias for the shared DurationFormatter implementation.
/// Dashboard uses InvariantCulture for consistent formatting.
/// </summary>
internal static class DurationFormatter
{
    public static string FormatDuration(TimeSpan duration) => Shared.DurationFormatter.FormatDuration(duration, CultureInfo.InvariantCulture);
    public static string GetUnit(TimeSpan duration) => Shared.DurationFormatter.GetUnit(duration);
}
