// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AppConfiguration.Tests;

public class DataAppConfigurationPublicApiTests
{
    [Fact]
    public void AddAzureAppConfigurationShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "appConfig";

        var action = () => builder.AddAzureAppConfiguration(connectionName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureAppConfigurationShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureAppConfiguration(connectionName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }
}
