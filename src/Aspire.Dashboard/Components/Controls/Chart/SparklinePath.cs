// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Dashboard.Components.Controls.Chart;

/// <summary>
/// Builds SVG path data for a small inline sparkline.
/// </summary>
public static class SparklinePath
{
    /// <summary>
    /// Builds a smoothed line path and a matching closed area path for the values.
    /// Values are spaced evenly across <paramref name="width"/>. Null values create gaps.
    /// Returns empty strings when there is nothing to draw.
    /// </summary>
    public static (string Line, string Area) Build(IReadOnlyList<double?> values, double width, double height)
    {
        double? min = null;
        double? max = null;
        foreach (var value in values)
        {
            if (value is { } v)
            {
                min = min is null ? v : Math.Min(min.Value, v);
                max = max is null ? v : Math.Max(max.Value, v);
            }
        }

        if (min is null || max is null || values.Count < 2)
        {
            return (string.Empty, string.Empty);
        }

        // Scale to the data's own range rather than from zero. A sparkline shows the shape of
        // the trend, and a gauge hovering around a large value would otherwise render flat.
        // A small vertical inset keeps the stroke from being clipped at the top and bottom.
        const double Inset = 2;
        var range = max.Value - min.Value;
        var step = width / (values.Count - 1);

        // A constant series sits on the baseline. Drawing it mid-height would read as a value
        // above zero, e.g. a counter that stays at 0 would look like it had a steady non-zero rate.
        double ScaleY(double value) => range == 0
            ? height - Inset
            : Inset + (height - (2 * Inset)) * (1 - ((value - min.Value) / range));

        var line = new StringBuilder();
        var area = new StringBuilder();
        var segment = new List<(double X, double Y)>();

        for (var i = 0; i <= values.Count; i++)
        {
            if (i < values.Count && values[i] is { } v)
            {
                segment.Add((i * step, ScaleY(v)));
                continue;
            }

            if (segment.Count > 0)
            {
                AppendSegment(line, area, segment, height);
                segment.Clear();
            }
        }

        return (line.ToString(), area.ToString());
    }

    private static void AppendSegment(StringBuilder line, StringBuilder area, List<(double X, double Y)> points, double height)
    {
        var curve = new StringBuilder();
        curve.Append(CultureInfo.InvariantCulture, $"M{points[0].X:0.##},{points[0].Y:0.##}");

        if (points.Count == 1)
        {
            // A lone point has no length to stroke, so draw a short horizontal tick for it.
            curve.Append(CultureInfo.InvariantCulture, $"h0.5");
        }
        else
        {
            var tangents = CalculateMonotoneTangents(points);
            for (var i = 0; i < points.Count - 1; i++)
            {
                var (x0, y0) = points[i];
                var (x1, y1) = points[i + 1];
                var dx = (x1 - x0) / 3;
                curve.Append(CultureInfo.InvariantCulture,
                    $"C{x0 + dx:0.##},{y0 + (dx * tangents[i]):0.##} {x1 - dx:0.##},{y1 - (dx * tangents[i + 1]):0.##} {x1:0.##},{y1:0.##}");
            }
        }

        line.Append(curve);
        area.Append(curve);
        area.Append(CultureInfo.InvariantCulture, $"L{points[^1].X:0.##},{height:0.##}L{points[0].X:0.##},{height:0.##}Z");
    }

    // Monotone cubic interpolation (Fritsch–Carlson, as used by d3's curveMonotoneX).
    // Unlike Catmull-Rom splines it never overshoots the data, so a flat run of zeros stays
    // flat and a step change doesn't produce a misleading bump above or below the real values.
    // See https://en.wikipedia.org/wiki/Monotone_cubic_interpolation and
    // https://github.com/d3/d3-shape/blob/main/src/curve/monotone.js
    private static double[] CalculateMonotoneTangents(List<(double X, double Y)> points)
    {
        var n = points.Count;
        var slopes = new double[n - 1];
        for (var i = 0; i < n - 1; i++)
        {
            slopes[i] = (points[i + 1].Y - points[i].Y) / (points[i + 1].X - points[i].X);
        }

        var tangents = new double[n];
        tangents[0] = slopes[0];
        tangents[n - 1] = slopes[n - 2];

        for (var i = 1; i < n - 1; i++)
        {
            var s0 = slopes[i - 1];
            var s1 = slopes[i];
            if (s0 * s1 <= 0)
            {
                // Local extremum or flat section: a zero tangent prevents overshoot.
                tangents[i] = 0;
                continue;
            }

            var h0 = points[i].X - points[i - 1].X;
            var h1 = points[i + 1].X - points[i].X;
            var p = ((s0 * h1) + (s1 * h0)) / (h0 + h1);
            tangents[i] = Math.Sign(s0) * Math.Min(Math.Min(Math.Abs(s0), Math.Abs(s1)), 0.5 * Math.Abs(p)) * 2;
        }

        return tangents;
    }
}
