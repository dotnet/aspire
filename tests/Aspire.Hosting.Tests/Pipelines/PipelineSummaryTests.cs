// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001

using Aspire.Hosting.Pipelines;

namespace Aspire.Hosting.Tests.Pipelines;

public class PipelineSummaryTests
{
    [Fact]
    public void Add_WithValidKeyAndValue_AddsItemToCollection()
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act
        summary.Add("Key1", "Value1");

        // Assert
        Assert.Single(summary.Items);
        Assert.Equal("Key1", summary.Items[0].Key);
        Assert.Equal("Value1", summary.Items[0].Value);
    }

    [Fact]
    public void Add_MultipleItems_PreservesInsertionOrder()
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act
        summary.Add("First", "1");
        summary.Add("Second", "2");
        summary.Add("Third", "3");

        // Assert
        Assert.Equal(3, summary.Items.Count);
        Assert.Equal("First", summary.Items[0].Key);
        Assert.Equal("Second", summary.Items[1].Key);
        Assert.Equal("Third", summary.Items[2].Key);
    }

    [Fact]
    public void Add_DuplicateKeys_AllowsBothInItems()
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act
        summary.Add("Key", "Value1");
        summary.Add("Key", "Value2");

        // Assert
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal("Value1", summary.Items[0].Value);
        Assert.Equal("Value2", summary.Items[1].Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_WithNullOrWhitespaceKey_ThrowsArgumentException(string? key)
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => summary.Add(key!, "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_WithNullOrWhitespaceValue_ThrowsArgumentException(string? value)
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => summary.Add("key", value!));
    }

    [Fact]
    public void Items_ReturnsReadOnlyCollection()
    {
        // Arrange
        var summary = new PipelineSummary();
        summary.Add("Key", "Value");

        // Act
        var items = summary.Items;

        // Assert
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<KeyValuePair<string, string>>>(items);
    }

    [Fact]
    public void Items_WhenEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act & Assert
        Assert.Empty(summary.Items);
    }

    [Fact]
    public void Items_WithItems_ReturnsItemsInInsertionOrder()
    {
        // Arrange
        var summary = new PipelineSummary();
        summary.Add("Key1", "Value1");
        summary.Add("Key2", "Value2");

        // Act & Assert
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal(new KeyValuePair<string, string>("Key1", "Value1"), summary.Items[0]);
        Assert.Equal(new KeyValuePair<string, string>("Key2", "Value2"), summary.Items[1]);
    }

    [Fact]
    public void Items_WithDuplicateKeys_PreservesAllEntries()
    {
        // Arrange
        var summary = new PipelineSummary();
        summary.Add("Key", "FirstValue");
        summary.Add("Key", "LastValue");

        // Act & Assert
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal(new KeyValuePair<string, string>("Key", "FirstValue"), summary.Items[0]);
        Assert.Equal(new KeyValuePair<string, string>("Key", "LastValue"), summary.Items[1]);
    }

    [Fact]
    public void Add_WithUnicodeCharactersInKey_Succeeds()
    {
        // Arrange
        var summary = new PipelineSummary();

        // Act
        summary.Add("☁️ Target", "Azure");
        summary.Add("📦 Resource Group", "rg-test");

        // Assert
        Assert.Equal(2, summary.Items.Count);
        Assert.Equal("☁️ Target", summary.Items[0].Key);
        Assert.Equal("📦 Resource Group", summary.Items[1].Key);
    }
}
