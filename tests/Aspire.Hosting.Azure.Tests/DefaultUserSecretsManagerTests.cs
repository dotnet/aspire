// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning.Internal;

namespace Aspire.Hosting.Azure.Tests;

public class DefaultUserSecretsManagerTests
{
    [Fact]
    public void FlattenJsonObject_HandlesNestedStructures()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["Azure:SubscriptionId"] = "existing-flat-value",
            ["Azure"] = new JsonObject
            {
                ["Tenant"] = "microsoft.onmicrosoft.com",
                ["Deployments"] = new JsonObject
                {
                    ["MyStorage"] = new JsonObject
                    {
                        ["Id"] = "/subscriptions/123/deployments/MyStorage",
                        ["Parameters"] = "{ \"param\": \"value\" }"
                    }
                }
            }
        };

        // Use reflection to access the private method for testing
        var method = typeof(DefaultUserSecretsManager).GetMethod("FlattenJsonObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (JsonObject)method!.Invoke(null, [userSecrets])!;

        // Assert
        Assert.Equal("existing-flat-value", result["Azure:SubscriptionId"]!.ToString());
        Assert.Equal("microsoft.onmicrosoft.com", result["Azure:Tenant"]!.ToString());
        Assert.Equal("/subscriptions/123/deployments/MyStorage", result["Azure:Deployments:MyStorage:Id"]!.ToString());
        Assert.Equal("{ \"param\": \"value\" }", result["Azure:Deployments:MyStorage:Parameters"]!.ToString());
        
        // Verify no nested structures remain
        Assert.False(result.ContainsKey("Azure"), "Should not have nested 'Azure' object");
    }

    [Fact]
    public void FlattenJsonObject_HandlesSimpleFlatStructure()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["Azure:SubscriptionId"] = "07268dd7-4c50-434b-b1ff-67b8164edb41",
            ["Azure:Tenant"] = "microsoft.onmicrosoft.com",
            ["Azure:Location"] = "eastus2"
        };

        // Use reflection to access the private method for testing
        var method = typeof(DefaultUserSecretsManager).GetMethod("FlattenJsonObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (JsonObject)method!.Invoke(null, [userSecrets])!;

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("07268dd7-4c50-434b-b1ff-67b8164edb41", result["Azure:SubscriptionId"]!.ToString());
        Assert.Equal("microsoft.onmicrosoft.com", result["Azure:Tenant"]!.ToString());
        Assert.Equal("eastus2", result["Azure:Location"]!.ToString());
    }

    [Fact]
    public void FlattenJsonObject_HandlesEmptyObject()
    {
        // Arrange
        var userSecrets = new JsonObject();

        // Use reflection to access the private method for testing
        var method = typeof(DefaultUserSecretsManager).GetMethod("FlattenJsonObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (JsonObject)method!.Invoke(null, [userSecrets])!;

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FlattenJsonObject_HandlesDeeplyNestedStructures()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["Level1"] = new JsonObject
            {
                ["Level2"] = new JsonObject
                {
                    ["Level3"] = new JsonObject
                    {
                        ["DeepValue"] = "nested-value"
                    }
                }
            }
        };

        // Use reflection to access the private method for testing
        var method = typeof(DefaultUserSecretsManager).GetMethod("FlattenJsonObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (JsonObject)method!.Invoke(null, [userSecrets])!;

        // Assert
        Assert.Single(result);
        Assert.Equal("nested-value", result["Level1:Level2:Level3:DeepValue"]!.ToString());
    }

    [Fact]
    public void FlattenJsonObject_PreservesNullAndPrimitiveValues()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["StringValue"] = "text",
            ["NumberValue"] = 42,
            ["BoolValue"] = true,
            ["NullValue"] = null,
            ["Nested"] = new JsonObject
            {
                ["InnerString"] = "inner-text",
                ["InnerNull"] = null
            }
        };

        // Use reflection to access the private method for testing
        var method = typeof(DefaultUserSecretsManager).GetMethod("FlattenJsonObject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = (JsonObject)method!.Invoke(null, [userSecrets])!;

        // Assert
        Assert.Equal("text", result["StringValue"]!.ToString());
        Assert.Equal("42", result["NumberValue"]!.ToString());
        Assert.Equal("True", result["BoolValue"]!.ToString());
        Assert.Null(result["NullValue"]);
        Assert.Equal("inner-text", result["Nested:InnerString"]!.ToString());
        Assert.Null(result["Nested:InnerNull"]);
    }
}