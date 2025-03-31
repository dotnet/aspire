// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.AppConfiguration.Tests;

public class DataAppConfigurationPublicApiTests
{
    [Fact]
    public void AddAzureKeyVaultClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "appConfig";

        var action = () => builder.AddAzureAppConfigurationClient(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureAppConfigurationClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureAppConfigurationClient(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureAppConfigurationClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "appConfig";

        var action = () => builder.AddKeyedAzureAppConfigurationClient(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureAppConfigurationClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureAppConfigurationClient(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddAzureAppConfigurationShouldThrowWhenConfigurationManagerIsNull()
    {
        IConfigurationManager configurationManager = null!;
        const string connectionName = "appConfig";

        var action = () => configurationManager.AddAzureAppConfiguration(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configurationManager), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureAppConfigurationShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var configurationManager = new ConfigurationManager();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => configurationManager.AddAzureAppConfiguration(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }
}
