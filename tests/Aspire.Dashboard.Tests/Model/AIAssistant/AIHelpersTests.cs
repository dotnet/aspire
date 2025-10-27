// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model.Assistant;
using Xunit;

namespace Aspire.Dashboard.Tests.Model.AIAssistant;

public class AIHelpersTests
{
    [Fact]
    public void LimitLength_UnderLimit_ReturnFullValue()
    {
        var value = AIHelpers.LimitLength("How now brown cow?");
        Assert.Equal("How now brown cow?", value);
    }

    [Fact]
    public void LimitLength_OverLimit_ReturnTrimmedValue()
    {
        var value = AIHelpers.LimitLength(new string('!', 10_000));
        Assert.Equal($"{new string('!', AIHelpers.MaximumStringLength)}...[TRUNCATED]", value);
    }

    [Fact]
    public void GetLimitFromEndWithSummary_UnderLimits_ReturnAll()
    {
        // Arrange
        var values = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            values.Add(new string((char)('a' + i), 16));
        }

        // Act
        var (items, message) = AIHelpers.GetLimitFromEndWithSummary(values, totalValues: values.Count, limit: 20, "test item", s => s, s => ((string)s).Length);

        // Assert
        Assert.Equal(10, items.Count);
        Assert.Equal("Returned 10 test items.", message);
    }

    [Fact]
    public void GetLimitFromEndWithSummary_UnderTotal_ReturnPassedIn()
    {
        // Arrange
        var values = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            values.Add(new string((char)('a' + i), 16));
        }

        // Act
        var (items, message) = AIHelpers.GetLimitFromEndWithSummary(values, totalValues: 100, limit: 20, "test item", s => s, s => ((string)s).Length);

        // Assert
        Assert.Equal(10, items.Count);
        Assert.Equal("Returned latest 10 test items. Earlier 90 test items not returned because of size limits.", message);
    }

    [Fact]
    public void GetLimitFromEndWithSummary_ExceedCountLimit_ReturnMostRecentItems()
    {
        // Arrange
        var values = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            values.Add(new string((char)('a' + i), 2));
        }

        // Act
        var (items, message) = AIHelpers.GetLimitFromEndWithSummary(values, totalValues: 100, limit: 5, "test item", s => s, s => ((string)s).Length);

        // Assert
        Assert.Collection(items,
            s => Assert.Equal("ff", s),
            s => Assert.Equal("gg", s),
            s => Assert.Equal("hh", s),
            s => Assert.Equal("ii", s),
            s => Assert.Equal("jj", s));
        Assert.Equal("Returned latest 5 test items. Earlier 95 test items not returned because of size limits.", message);
    }

    [Fact]
    public void GetLimitFromEndWithSummary_ExceedTokenLimit_ReturnMostRecentItems()
    {
        const int textLength = 1024 * 2;

        // Arrange
        var values = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            values.Add(new string((char)('a' + i), textLength));
        }

        // Act
        var (items, message) = AIHelpers.GetLimitFromEndWithSummary(values, limit: 10, "test item", s => s, s => ((string)s).Length);

        // Assert
        Assert.Collection(items,
            s => Assert.Equal(new string('g', textLength), s),
            s => Assert.Equal(new string('h', textLength), s),
            s => Assert.Equal(new string('i', textLength), s),
            s => Assert.Equal(new string('j', textLength), s));
        Assert.Equal("Returned latest 4 test items. Earlier 6 test items not returned because of size limits.", message);
    }
}
