// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Confluent.Kafka.Tests;

public class KafkaProducerSettingsTests
{
    [Fact]
    public void ConsolidateEmptyConnectionString_ShouldNotOverwriteBootstrapServers()
    {
        // Arrange
        var settings = new KafkaProducerSettings
        {
            ConnectionString = string.Empty
        };
        settings.Config.BootstrapServers = "broker1:9092,broker2:9092";

        // Act
        settings.Consolidate();

        // Assert
        Assert.Equal("broker1:9092,broker2:9092", settings.Config.BootstrapServers);
    }

    [Fact]
    public void ConsolidateNullConnectionString_ShouldNotOverwriteBootstrapServers()
    {
        // Arrange
        var settings = new KafkaProducerSettings
        {
            ConnectionString = null
        };
        settings.Config.BootstrapServers = "broker1:9092,broker2:9092";

        // Act
        settings.Consolidate();

        // Assert
        Assert.Equal("broker1:9092,broker2:9092", settings.Config.BootstrapServers);
    }

    [Fact]
    public void ConsolidateValidConnectionString_ShouldOverwriteBootstrapServers()
    {
        // Arrange
        var settings = new KafkaProducerSettings
        {
            ConnectionString = "new-broker:9092"
        };
        settings.Config.BootstrapServers = "broker1:9092,broker2:9092";

        // Act
        settings.Consolidate();

        // Assert
        Assert.Equal("new-broker:9092", settings.Config.BootstrapServers);
    }

    [Fact]
    public void ValidateEmptyBootstrapServers_ShouldThrowException()
    {
        // Arrange
        var settings = new KafkaProducerSettings();
        settings.Config.BootstrapServers = string.Empty;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());
        Assert.Equal("No bootstrap servers configured.", exception.Message);
    }

    [Fact]
    public void ValidateNullBootstrapServers_ShouldThrowException()
    {
        // Arrange
        var settings = new KafkaProducerSettings();
        settings.Config.BootstrapServers = null;

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => settings.Validate());
        Assert.Equal("No bootstrap servers configured.", exception.Message);
    }
}