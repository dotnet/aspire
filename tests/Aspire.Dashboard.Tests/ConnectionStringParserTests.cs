// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Xunit;

namespace Aspire.Dashboard.Tests;

public class ConnectionStringParserTests
{
    [Theory]
    [InlineData("redis://[fe80::1]:6380", true, "fe80::1", 6380)]
    [InlineData("postgres://h/db", true, "h", 5432)]
    [InlineData("Endpoint=h:6379;password=pw", true, "h", 6379)]
    [InlineData("host=h;user=foo", true, "h", null)]
    [InlineData("broker1:9092,broker2:9092", true, "broker1", 9092)]
    [InlineData("/var/sqlite/file.db", false, "", null)]
    [InlineData("foo bar baz", false, "", null)]
    [InlineData("https://models.github.ai/inference", true, "models.github.ai", 443)]
    [InlineData("Server=tcp:localhost,1433;Database=test", true, "localhost", 1433)]
    [InlineData("Server=localhost;port=5432", true, "localhost", 5432)]
    public void TryDetectHostAndPort_VariousFormats_ReturnsExpectedResults(
        string connectionString, 
        bool expectedResult, 
        string expectedHost, 
        int? expectedPort)
    {
        // Act
        var result = ConnectionStringParser.TryDetectHostAndPort(connectionString, out var host, out var port);
        
        // Assert
        Assert.Equal(expectedResult, result);
        if (expectedResult)
        {
            Assert.Equal(expectedHost, host);
            Assert.Equal(expectedPort, port);
        }
        else
        {
            Assert.Null(host);
            Assert.Null(port);
        }
    }

    [Fact]
    public void TryDetectHostAndPort_IPv6URI_ReturnsCorrectHost()
    {
        // Test case specifically for IPv6 addresses with brackets
        var connectionString = "redis://[fe80::1]:6380";
        var result = ConnectionStringParser.TryDetectHostAndPort(connectionString, out var host, out var port);
        
        Assert.True(result);
        Assert.Equal("fe80::1", host); // Brackets should be trimmed
        Assert.Equal(6380, port);
    }

    [Fact]
    public void TryDetectHostAndPort_KeyValuePairsWithSemicolon_ParsesCorrectly()
    {
        var connectionString = "Endpoint=h:6379;password=pw;database=0";
        var result = ConnectionStringParser.TryDetectHostAndPort(connectionString, out var host, out var port);
        
        Assert.True(result);
        Assert.Equal("h", host);
        Assert.Equal(6379, port);
    }

    [Fact]
    public void TryDetectHostAndPort_DelimitedList_TakesFirstEntry()
    {
        var connectionString = "broker1:9092,broker2:9093,broker3:9094";
        var result = ConnectionStringParser.TryDetectHostAndPort(connectionString, out var host, out var port);
        
        Assert.True(result);
        Assert.Equal("broker1", host);
        Assert.Equal(9092, port);
    }
}