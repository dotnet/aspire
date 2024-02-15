// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Components.Controls;
using Aspire.Dashboard.Otlp.Model.MetricValues;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class MetricsTests
{
    [Theory]
    [MemberData(nameof(GetMetricsUpdateCases))]
    public void MetricsUpdateTests(
        List<DimensionScope>? matchedDimensions,
        List<MetricTable.MetricView> currentMetrics,
        bool isHistogramMetric,
        bool showCount,
        bool onlyShowValueChanges,
        List<MetricTable.MetricView> expectedMetrics,
        List<int> expectedAddedIndices,
        bool expectedAnyDimensionsShown)
    {
        MetricTable.UpdateMetrics(
            matchedDimensions,
            currentMetrics,
            isHistogramMetric,
            showCount,
            onlyShowValueChanges,
            out _,
            out var actualAddedIndices,
            out var actualAnyDimensionsShown);

        Assert.Equal(expectedMetrics, currentMetrics);
        Assert.Equal(expectedAddedIndices, actualAddedIndices.AsEnumerable().Reverse()); // reverse because we show newest values first
        Assert.Equal(expectedAnyDimensionsShown, actualAnyDimensionsShown);
    }

    public static IEnumerable<object?[]> GetMetricsUpdateCases()
    {
        yield return NoExistingMetrics();
        yield return ExistingMetrics();
        yield return NoMetrics();

        yield break;

        static object?[] NoExistingMetrics()
        {
            var now = DateTime.Now;

            var dimensionA = new DimensionScope([new KeyValuePair<string, string>("a", "a")]) { Name = "a" };
            var dimensionB = new DimensionScope([new KeyValuePair<string, string>("a", "b")]) { Name = "b" };

            dimensionA.Values.Add(new MetricValue<double>(1, now, now.AddSeconds(5)) { Count = 1 });
            dimensionA.Values.Add(new MetricValue<double>(-1, now.AddSeconds(5), now.AddSeconds(10)) { Count = 4 });

            dimensionB.Values.Add(new MetricValue<double>(0, now.AddSeconds(10), now.AddSeconds(11)) { Count = 2 });

            List<DimensionScope> dimensions = [dimensionA, dimensionB];
            List<MetricTable.MetricView> currentMetrics = [];
            return CreateCase(
                dimensions,
                currentMetrics,
                false,
                false,
                false,
                [
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Constant,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Value = dimensionA.Values[0],
                        ValueChange = MetricTable.ValueDirectionChange.Constant
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Up,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Value = dimensionA.Values[1],
                        ValueChange = MetricTable.ValueDirectionChange.Down
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Down,
                        DimensionAttributes = dimensionB.Attributes,
                        DimensionName = dimensionB.Name,
                        Value = dimensionB.Values[0],
                        ValueChange = MetricTable.ValueDirectionChange.Up
                    }
                ],
                [0, 1, 2],
                true);
        }

        static object?[] ExistingMetrics()
        {
            var now = DateTime.Now;

            var dimensionA = new DimensionScope([new KeyValuePair<string, string>("a", "a")]) { Name = "a" };
            var dimensionB = new DimensionScope([
                new KeyValuePair<string, string>("a", "b"), new KeyValuePair<string, string>("b", "c")
            ]) { Name = "b" };

            dimensionA.Values.Add(new MetricValue<double>(1, now, now.AddSeconds(5)) { Count = 1 });
            dimensionA.Values.Add(new MetricValue<double>(2, now.AddSeconds(5), now.AddSeconds(10)) { Count = 1 });
            dimensionA.Values.Add(new MetricValue<double>(4, now.AddSeconds(12), now.AddSeconds(16)) { Count = 1 });

            dimensionB.Values.Add(new MetricValue<double>(4, now.AddSeconds(10), now.AddSeconds(11)) { Count = 2 });
            dimensionB.Values.Add(new MetricValue<double>(2, now.AddSeconds(17), now.AddSeconds(19)) { Count = 2 });

            List<DimensionScope> dimensions = [dimensionA, dimensionB];

            List<MetricTable.MetricView> currentMetrics = [
                new MetricTable.MetricView
                {
                    CountChange = MetricTable.ValueDirectionChange.Constant,
                    DimensionAttributes = dimensionA.Attributes,
                    DimensionName = dimensionA.Name,
                    Value = dimensionA.Values[0],
                    ValueChange = MetricTable.ValueDirectionChange.Constant
                },
                new MetricTable.MetricView
                {
                    CountChange = MetricTable.ValueDirectionChange.Up,
                    DimensionAttributes = dimensionA.Attributes,
                    DimensionName = dimensionA.Name,
                    Value = new MetricValue<double>(-1, now.AddSeconds(5), now.AddSeconds(10)) { Count = 4 },
                    ValueChange = MetricTable.ValueDirectionChange.Down
                },
                new MetricTable.MetricView
                {
                    CountChange = MetricTable.ValueDirectionChange.Down,
                    DimensionAttributes = dimensionB.Attributes,
                    DimensionName = dimensionB.Name,
                    Value = new MetricValue<double>(0, now.AddSeconds(10), now.AddSeconds(11)) { Count = 2 },
                    ValueChange = MetricTable.ValueDirectionChange.Down
                }];

            return CreateCase(
                dimensions,
                currentMetrics,
                false,
                false,
                false,
                [
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Constant,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Value = dimensionA.Values[0],
                        ValueChange = MetricTable.ValueDirectionChange.Constant
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Constant,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Value = dimensionA.Values[1],
                        ValueChange = MetricTable.ValueDirectionChange.Up
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Up,
                        DimensionAttributes = dimensionB.Attributes,
                        DimensionName = dimensionB.Name,
                        Value = dimensionB.Values[0],
                        ValueChange = MetricTable.ValueDirectionChange.Up
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Down,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Value = dimensionA.Values[2],
                        ValueChange = MetricTable.ValueDirectionChange.Constant
                    },
                    new MetricTable.MetricView
                    {
                        CountChange = MetricTable.ValueDirectionChange.Up,
                        DimensionAttributes = dimensionB.Attributes,
                        DimensionName = dimensionB.Name,
                        Value = dimensionB.Values[1],
                        ValueChange = MetricTable.ValueDirectionChange.Down
                    }
                ],
                [0, 1, 2], // since we add to the beginning of metrics, we added 2 + modified the value previously at index-0
                true);
        }

        static object?[] NoMetrics()
        {
            var dimensionA = new DimensionScope([new KeyValuePair<string, string>("a", "a")]) { Name = DimensionScope.NoDimensions };

            return CreateCase(
                [dimensionA],
                [
                    new MetricTable.HistogramMetricView
                    {
                        CountChange = null,
                        DimensionAttributes = dimensionA.Attributes,
                        DimensionName = dimensionA.Name,
                        Percentiles = [],
                        ValueChange = null,
                        Value = new HistogramValue([], 1, 1, DateTime.Now, DateTime.Now, [])
                    }
                ],
                false,
                false,
                false,
                [],
                [],
                false);
        }

        static object?[] CreateCase(
            List<DimensionScope>? matchedDimensions,
            List<MetricTable.MetricView> currentMetrics,
            bool isHistogram,
            bool showCount,
            bool onlyShowValueChanges,
            List<MetricTable.MetricView> expectedMetrics,
            List<int> expectedAddedIndices,
            bool expectedAnyDimensionsShown)
        {
            return
            [
                matchedDimensions,
                currentMetrics,
                isHistogram,
                showCount,
                onlyShowValueChanges,
                expectedMetrics,
                expectedAddedIndices,
                expectedAnyDimensionsShown
            ];
        }
    }
}
