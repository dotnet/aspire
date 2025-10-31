// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Aspire.Hosting.Publishing.Internal;
using Aspire.Hosting.UserSecrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

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

        var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
        manager?.GetOrSetSecret(configuration, key, () => value);
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

        var manager = UserSecretsManagerFactory.Instance.Create(testAssembly);
        manager?.GetOrSetSecret(configuration, key, TokenGenerator.GenerateToken);
        var userSecrets = GetUserSecrets(userSecretsId);

        Assert.False(userSecrets.TryGetValue(key, out var savedValue));

        DeleteUserSecretsFile(userSecretsId);
    }

    private static Dictionary<string, string?> GetUserSecrets(string userSecretsId)
    {
        var manager = UserSecretsManagerFactory.Instance.CreateFromId(userSecretsId);
        if (manager == null || !File.Exists(manager.FilePath))
        {
            return new Dictionary<string, string?>();
        }

        var config = new ConfigurationBuilder()
            .AddJsonFile(manager.FilePath, optional: true)
            .Build();

        return config.AsEnumerable()
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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
}
