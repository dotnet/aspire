// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Azure.Tests;

public class JsonExtensionsTests
{
    [Fact]
    public void Prop_ReturnsExistingNode_WhenNodeAlreadyExists()
    {
        // Arrange
        var rootJson = new JsonObject();
        var azureNode = rootJson.Prop("Azure");
        azureNode.AsObject()["TestProperty"] = "TestValue";

        // Act
        var retrievedNode = rootJson.Prop("Azure");

        // Assert
        Assert.Same(azureNode, retrievedNode);
        Assert.Equal("TestValue", retrievedNode["TestProperty"]!.GetValue<string>());
    }

    [Fact]
    public void Prop_CreatesNewNode_WhenNodeDoesNotExist()
    {
        // Arrange
        var rootJson = new JsonObject();

        // Act
        var newNode = rootJson.Prop("NewProperty");

        // Assert
        Assert.NotNull(newNode);
        Assert.Same(rootJson["NewProperty"], newNode);
    }

    [Fact]
    public void Prop_NestedAccess_CreatesHierarchy()
    {
        // Arrange
        var rootJson = new JsonObject();

        // Act
        var deeply = rootJson.Prop("Level1")
                              .Prop("Level2")
                              .Prop("Level3")
                              .Prop("Level4");

        // Assert
        Assert.NotNull(rootJson["Level1"]);
        Assert.NotNull(rootJson["Level1"]!["Level2"]);
        Assert.NotNull(rootJson["Level1"]!["Level2"]!["Level3"]);
        Assert.NotNull(rootJson["Level1"]!["Level2"]!["Level3"]!["Level4"]);
        Assert.Same(deeply, rootJson["Level1"]!["Level2"]!["Level3"]!["Level4"]);
    }
}

