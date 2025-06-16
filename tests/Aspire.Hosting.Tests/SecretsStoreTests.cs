// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.SecretManager.Tools.Internal;

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
}
