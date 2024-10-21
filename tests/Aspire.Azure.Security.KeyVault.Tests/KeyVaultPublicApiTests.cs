// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core.Extensions;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Security.KeyVault.Tests;

public class KeyVaultPublicApiTests
{
    [Fact]
    public void AddAzureKeyVaultClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "keyvault";

        var action = () => builder.AddAzureKeyVaultClient(
            connectionName,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureKeyVaultClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureKeyVaultClient(
            connectionName,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureKeyVaultClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "keyvault";

        var action = () => builder.AddKeyedAzureKeyVaultClient(
            name,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureKeyVaultClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureKeyVaultClient(
            name,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<IAzureClientBuilder<SecretClient, SecretClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzureKeyVaultSecretsShouldThrowWhenConfigurationManagerIsNull()
    {
        IConfigurationManager configurationManager = null!;
        const string connectionName = "secrets";

        var action = () => configurationManager.AddAzureKeyVaultSecrets(
            connectionName,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<SecretClientOptions>?),
            default(AzureKeyVaultConfigurationOptions?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configurationManager), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureKeyVaultSecretsShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IConfigurationManager configurationManager = new ConfigurationManager();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => configurationManager.AddAzureKeyVaultSecrets(
            connectionName,
            default(Action<AzureSecurityKeyVaultSettings>?),
            default(Action<SecretClientOptions>?),
            default(AzureKeyVaultConfigurationOptions?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }
}
