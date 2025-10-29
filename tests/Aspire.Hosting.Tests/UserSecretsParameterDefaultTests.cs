// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.SecretManager.Tools.Internal;

namespace Aspire.Hosting.Tests;

public class UserSecretsParameterDefaultTests
{
    private static readonly ConstructorInfo s_userSecretsIdAttrCtor = typeof(UserSecretsIdAttribute).GetConstructor([typeof(string)])!;

    [Fact]
    public void UserSecretsParameterDefault_GetDefaultValue_SavesValueInAppHostUserSecrets()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);
        var paramDefault = new TestParameterDefault();
        var userSecretDefault = new UserSecretsParameterDefault(testAssembly, "TestApplication.AppHost", "param1", paramDefault);
        var initialValue = userSecretDefault.GetDefaultValue();

        var userSecrets = GetUserSecrets(userSecretsId);
        Assert.Equal(initialValue, userSecrets["Parameters:param1"]);

        DeleteUserSecretsFile(userSecretsId);
    }

    [Fact]
    public void UserSecretsParameterDefault_GetDefaultValue_DoesntThrowIfValueCantBeSaved()
    {
        // Do not set a user secrets id attribute on the assembly
        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, []);
        var paramDefault = new TestParameterDefault();
        var userSecretDefault = new UserSecretsParameterDefault(testAssembly, "TestApplication.AppHost", "param1", paramDefault);

        var initialValue = userSecretDefault.GetDefaultValue();
        Assert.NotNull(initialValue);
    }

    [Fact]
    public void UserSecretsParameterDefault_GetDefaultValue_DoesntThrowIfSecretsFileContainsComments()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        DeleteUserSecretsFile(userSecretsId);
        var userSecretsPath = PathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        if (File.Exists(userSecretsPath))
        {
            File.Delete(userSecretsPath);
        }
        var secretsFileContents = """
            {
                // This is a comment in a JSON file
                "SomeConfigKey": "some value"
            }
            """;
        EnsureUserSecretsDirectory(userSecretsPath);
        File.WriteAllText(userSecretsPath, secretsFileContents, Encoding.UTF8);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);
        var paramDefault = new TestParameterDefault();
        var userSecretDefault = new UserSecretsParameterDefault(testAssembly, "TestApplication.AppHost", "param1", paramDefault);

        var _ = userSecretDefault.GetDefaultValue();
    }

    [Fact]
    public async Task TrySetUserSecret_ConcurrentWrites_PreservesAllSecrets()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);

        // Simulate concurrent writes from multiple threads (like SQL Server and RabbitMQ generating passwords)
        var tasks = new List<Task<bool>>();
        var secretsToWrite = new Dictionary<string, string>
        {
            ["Parameters:sqlserver-password"] = "SqlPassword123!",
            ["Parameters:rabbitmq-password"] = "RabbitPassword456!",
            ["Parameters:redis-password"] = "RedisPassword789!",
            ["Parameters:postgres-password"] = "PostgresPassword012!",
        };

        foreach (var kvp in secretsToWrite)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            tasks.Add(Task.Run(() => SecretsStore.TrySetUserSecret(testAssembly, key, value)));
        }

        var results = await Task.WhenAll(tasks);

        // All writes should succeed
        Assert.All(results, Assert.True);

        // All secrets should be preserved
        var userSecrets = GetUserSecrets(userSecretsId);
        foreach (var kvp in secretsToWrite)
        {
            Assert.True(userSecrets.ContainsKey(kvp.Key), $"Secret '{kvp.Key}' was not found in user secrets");
            Assert.Equal(kvp.Value, userSecrets[kvp.Key]);
        }

        DeleteUserSecretsFile(userSecretsId);
    }

    [Fact]
    public async Task TrySetUserSecret_ConcurrentWritesSameKey_LastWriteWins()
    {
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);

        // Simulate concurrent writes to the same key
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 10; i++)
        {
            var value = $"Value{i}";
            tasks.Add(Task.Run(() => SecretsStore.TrySetUserSecret(testAssembly, "Parameters:test-key", value)));
        }

        var results = await Task.WhenAll(tasks);

        // All writes should succeed
        Assert.All(results, Assert.True);

        // The key should exist with one of the values
        var userSecrets = GetUserSecrets(userSecretsId);
        Assert.True(userSecrets.ContainsKey("Parameters:test-key"));
        Assert.NotNull(userSecrets["Parameters:test-key"]);

        DeleteUserSecretsFile(userSecretsId);
    }

    private static void EnsureUserSecretsDirectory(string secretsFilePath)
    {
        var directoryName = Path.GetDirectoryName(secretsFilePath);
        if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }
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

    private sealed class TestParameterDefault : ParameterDefault
    {
        public override string GetDefaultValue()
        {
            return Guid.NewGuid().ToString("N");
        }

        public override void WriteToManifest(ManifestPublishingContext context)
        {
            throw new NotImplementedException();
        }
    }
}
