// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.SecretManager.Tools.Internal;
using Xunit;

namespace Aspire.Hosting.Tests;

public class SecretsStoreTests
{
    private static readonly ConstructorInfo s_userSecretsIdAttrCtor = typeof(UserSecretsIdAttribute).GetConstructor([typeof(string)])!;

    [Fact]
    public void GetOrSetUserSecret_SavesValueToUserSecrets()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("testhost"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);

        var key = "AppHost:OtlpApiKey";
        var configuration = new ConfigurationManager();
        var value = TokenGenerator.GenerateToken();

        SecretsStore.GetOrSetUserSecret(configuration, testAssembly, key, () => value);
        var userSecrets = GetUserSecrets(userSecretsId);

        var configValue = configuration[key];
        Assert.True(userSecrets.TryGetValue(key, out var savedValue));
        Assert.Equal(configValue, savedValue);

        DeleteUserSecretsFile(userSecretsId);
    }

    [Fact]
    public void GetOrSetUserSecret_ReadsValueFromConfiguration()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("testhost"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);

        var key = "AppHost:OtlpApiKey";
        var configuration = new ConfigurationManager();
        var valueInConfig = TokenGenerator.GenerateToken();
        configuration[key] = valueInConfig;

        SecretsStore.GetOrSetUserSecret(configuration, testAssembly, key, TokenGenerator.GenerateToken);
        var userSecrets = GetUserSecrets(userSecretsId);

        Assert.False(userSecrets.TryGetValue(key, out var savedValue));

        DeleteUserSecretsFile(userSecretsId);
    }

    private static Dictionary<string, string?> GetUserSecrets(string userSecretsId)
    {
        var secretsStore = new SecretsStore(userSecretsId);
        return secretsStore.AsEnumerable().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private static void ClearUsersSecrets(string userSecretsId)
    {
        var secretsStore = new SecretsStore(userSecretsId);
        secretsStore.Clear();
        secretsStore.Save();
    }

    private static void DeleteUserSecretsFile(string userSecretsId)
    {
        var userSecretsPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        if (File.Exists(userSecretsPath))
        {
            File.Delete(userSecretsPath);
        }
    }

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

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

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

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

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

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

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

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

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

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

        // Assert
        Assert.Equal("text", result["StringValue"]!.ToString());
        Assert.Equal("42", result["NumberValue"]!.ToString());
        Assert.Equal("true", result["BoolValue"]!.ToString());
        Assert.Null(result["NullValue"]);
        Assert.Equal("inner-text", result["Nested:InnerString"]!.ToString());
        Assert.Null(result["Nested:InnerNull"]);
    }

    [Fact]
    public void FlattenJsonObject_HandlesArraysWithPrimitiveValues()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["SimpleArray"] = new JsonArray("value1", "value2", "value3"),
            ["NumberArray"] = new JsonArray(1, 2, 3),
            ["MixedArray"] = new JsonArray("text", 42, true, null)
        };

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

        // Assert
        Assert.Equal("value1", result["SimpleArray:0"]!.ToString());
        Assert.Equal("value2", result["SimpleArray:1"]!.ToString());
        Assert.Equal("value3", result["SimpleArray:2"]!.ToString());
        
        Assert.Equal("1", result["NumberArray:0"]!.ToString());
        Assert.Equal("2", result["NumberArray:1"]!.ToString());
        Assert.Equal("3", result["NumberArray:2"]!.ToString());
        
        Assert.Equal("text", result["MixedArray:0"]!.ToString());
        Assert.Equal("42", result["MixedArray:1"]!.ToString());
        Assert.Equal("true", result["MixedArray:2"]!.ToString());
        Assert.Null(result["MixedArray:3"]);
    }

    [Fact]
    public void FlattenJsonObject_HandlesArraysWithObjects()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["ObjectArray"] = new JsonArray(
                new JsonObject { ["Name"] = "Item1", ["Value"] = "Value1" },
                new JsonObject { ["Name"] = "Item2", ["Value"] = "Value2" }
            ),
            ["NestedConfig"] = new JsonObject
            {
                ["Items"] = new JsonArray(
                    new JsonObject
                    {
                        ["Id"] = "1",
                        ["Settings"] = new JsonObject { ["Enabled"] = true }
                    }
                )
            }
        };

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

        // Assert
        Assert.Equal("Item1", result["ObjectArray:0:Name"]!.ToString());
        Assert.Equal("Value1", result["ObjectArray:0:Value"]!.ToString());
        Assert.Equal("Item2", result["ObjectArray:1:Name"]!.ToString());
        Assert.Equal("Value2", result["ObjectArray:1:Value"]!.ToString());
        
        Assert.Equal("1", result["NestedConfig:Items:0:Id"]!.ToString());
        Assert.Equal("true", result["NestedConfig:Items:0:Settings:Enabled"]!.ToString());
    }

    [Fact]
    public void FlattenJsonObject_HandlesEmptyArrays()
    {
        // Arrange
        var userSecrets = new JsonObject
        {
            ["EmptyArray"] = new JsonArray(),
            ["OtherValue"] = "test"
        };

        // Act
        var result = SecretsStore.FlattenJsonObject(userSecrets);

        // Assert
        Assert.Single(result);
        Assert.Equal("test", result["OtherValue"]!.ToString());
        Assert.False(result.ContainsKey("EmptyArray"));
    }

    [Fact]
    public async Task SetFromJsonObject_FlattensAndSetsSecrets()
    {
        // Arrange
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);
        
        var userSecrets = new JsonObject
        {
            ["Azure:SubscriptionId"] = "flat-value",
            ["Azure"] = new JsonObject
            {
                ["Tenant"] = "microsoft.onmicrosoft.com",
                ["Deployments"] = new JsonObject
                {
                    ["MyStorage"] = new JsonObject
                    {
                        ["Id"] = "/subscriptions/123/deployments/MyStorage"
                    }
                }
            }
        };

        // Act
        var secretsStore = new SecretsStore(userSecretsId);
        secretsStore.SetFromJsonObject(userSecrets);
        secretsStore.Save();

        // Assert
        var savedSecrets = GetUserSecrets(userSecretsId);
        Assert.Equal("flat-value", savedSecrets["Azure:SubscriptionId"]);
        Assert.Equal("microsoft.onmicrosoft.com", savedSecrets["Azure:Tenant"]);
        Assert.Equal("/subscriptions/123/deployments/MyStorage", savedSecrets["Azure:Deployments:MyStorage:Id"]);
        
        // Verify the flattened structure
        await Verify(savedSecrets);

        DeleteUserSecretsFile(userSecretsId);
    }

    [Fact]
    public async Task SecretsStore_WritesJsonToFile()
    {
        // Arrange
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);
        
        var userSecrets = new JsonObject
        {
            ["Parameters:token"] = "some=thing&looking=url&like=true",
            ["Parameters:password"] = "P+qMWNzkn*xm1rhXNF5st0",
            ["Azure"] = new JsonObject
            {
                ["Tenant"] = "microsoft.onmicrosoft.com",
                ["Deployments"] = new JsonObject
                {
                    ["MyStorage"] = new JsonObject
                    {
                        ["Id"] = "/subscriptions/123/deployments/MyStorage"
                    }
                }
            }
        };

        // Act
        var secretsStore = new SecretsStore(userSecretsId);
        secretsStore.SetFromJsonObject(userSecrets);
        secretsStore.Save();

        // Assert
        var secretsFilePath = secretsStore.SecretsFilePath;
        Assert.True(File.Exists(secretsFilePath));
        var fileContent = await File.ReadAllTextAsync(secretsFilePath);
        
        // Verify the content with snapshot testing
        await Verify(fileContent, "json");
        
        DeleteUserSecretsFile(userSecretsId);
    }

    [Fact]
    public async Task SecretsStore_HandlesComplexStructures()
    {
        // Arrange
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);
        
        var userSecrets = new JsonObject
        {
            ["Simple:Value"] = "test-value",
            ["Parameters:token"] = "value&with+special=characters",
            ["Azure"] = new JsonObject
            {
                ["SubscriptionId"] = "07268dd7-4c50-434b-b1ff-67b8164edb41",
                ["Tenant"] = "microsoft.onmicrosoft.com",
                ["Location"] = "eastus2",
                ["Deployments"] = new JsonObject
                {
                    ["Storage"] = new JsonObject
                    {
                        ["Id"] = "/subscriptions/123/deployments/MyStorage",
                        ["Parameters"] = "{ \"storageType\": \"Standard_LRS\" }",
                        ["ConnectionStrings"] = new JsonArray(
                            "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=abc123==",
                            "Server=tcp:test.database.windows.net;Database=TestDB"
                        )
                    }
                }
            }
        };

        // Act
        var secretsStore = new SecretsStore(userSecretsId);
        secretsStore.SetFromJsonObject(userSecrets);
        secretsStore.Save();

        // Assert
        var secretsFilePath = secretsStore.SecretsFilePath;
        Assert.True(File.Exists(secretsFilePath));
        var fileContent = await File.ReadAllTextAsync(secretsFilePath);
        
        // Verify the flattened structure and proper JSON encoding
        await Verify(fileContent, "json");
        
        DeleteUserSecretsFile(userSecretsId);
    }
}
