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
    public async Task SecretsStore_Save_DoesNotEscapeAmpersandAndPlusCharacters()
    {
        // This test verifies that SecretsStore.Save() doesn't escape & and + characters
        var userSecretsId = Guid.NewGuid().ToString("N");
        DeleteUserSecretsFile(userSecretsId);

        var secretsStore = new SecretsStore(userSecretsId);
        secretsStore.Set("Parameters:token", "some=thing&looking=url&like=true");
        secretsStore.Set("Parameters:password", "P+qMWNzkn*xm1rhXNF5st0");

        // Save to file 
        secretsStore.Save();

        // Read the file content directly to verify encoding
        var secretsPath = secretsStore.SecretsFilePath;
        var fileContent = await File.ReadAllTextAsync(secretsPath);

        // Verify the content with snapshot testing
        await Verify(fileContent, "json");

        // Clean up
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
