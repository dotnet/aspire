// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Publishing.Internal;
using Aspire.Hosting.UserSecrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

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
        var userSecretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
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
            tasks.Add(Task.Run(() =>
            {
                var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
                return manager?.TrySetSecret(key, value) ?? false;
            }));
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
    public async Task TrySetUserSecret_SqlServerAndRabbitMQ_BothSecretsPreserved()
    {
        // This test specifically reproduces the issue described in the bug report
        var userSecretsId = Guid.NewGuid().ToString("N");
        ClearUsersSecrets(userSecretsId);

        var testAssembly = AssemblyBuilder.DefineDynamicAssembly(
            new("TestAssembly"), AssemblyBuilderAccess.RunAndCollect, [new CustomAttributeBuilder(s_userSecretsIdAttrCtor, [userSecretsId])]);

        // Simulate SQL Server and RabbitMQ generating passwords concurrently
        var sqlTask = Task.Run(() =>
        {
            var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
            return manager?.TrySetSecret("Parameters:sql-password", "SqlPassword123!") ?? false;
        });
        var rabbitTask = Task.Run(() =>
        {
            var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
            return manager?.TrySetSecret("Parameters:rabbit-password", "RabbitPassword456!") ?? false;
        });

        var results = await Task.WhenAll(sqlTask, rabbitTask);

        // Both writes should succeed
        Assert.All(results, Assert.True);

        // Both secrets should be in the file
        var userSecrets = GetUserSecrets(userSecretsId);
        Assert.True(userSecrets.ContainsKey("Parameters:sql-password"), "SQL Server password was not found");
        Assert.True(userSecrets.ContainsKey("Parameters:rabbit-password"), "RabbitMQ password was not found");
        Assert.Equal("SqlPassword123!", userSecrets["Parameters:sql-password"]);
        Assert.Equal("RabbitPassword456!", userSecrets["Parameters:rabbit-password"]);

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
            tasks.Add(Task.Run(() =>
            {
                var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
                return manager?.TrySetSecret("Parameters:test-key", value) ?? false;
            }));
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
        var manager = UserSecretsManagerFactory.Instance.GetOrCreateFromId(userSecretsId);
        if (manager == null)
        {
            return new Dictionary<string, string?>();
        }
        
        // Read the secrets file directly
        var secrets = new Dictionary<string, string?>();
        if (File.Exists(manager.FilePath))
        {
            var json = File.ReadAllText(manager.FilePath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var config = new ConfigurationBuilder()
                    .AddJsonFile(manager.FilePath, optional: true)
                    .Build();
                    
                foreach (var kvp in config.AsEnumerable())
                {
                    if (kvp.Value != null)
                    {
                        secrets[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        return secrets;
    }

    private static void ClearUsersSecrets(string userSecretsId)
    {
        var filePath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static void DeleteUserSecretsFile(string userSecretsId)
    {
        var userSecretsPath = UserSecretsPathHelper.GetSecretsPathFromSecretsId(userSecretsId);
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

