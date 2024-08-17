// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.OpenAI;
using Azure.Core.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AzureAIOpenAIPublicApiTests
{
    [Fact]
    public void AddAzureOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string connectionName = "openai";

        var action = () => builder.AddAzureOpenAIClient(
            connectionName,
            default(Action<AzureOpenAISettings>?),
            default(Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAzureOpenAIClientShouldThrowWhenConnectionNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = isNull ? null! : string.Empty;

        var action = () => builder.AddAzureOpenAIClient(
            connectionName,
            default(Action<AzureOpenAISettings>?),
            default(Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "openai";

        var action = () => builder.AddKeyedAzureOpenAIClient(
            name,
            default(Action<AzureOpenAISettings>?),
            default(Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>?));

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddKeyedAzureOpenAIClient(
            name,
            default(Action<AzureOpenAISettings>?),
            default(Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>?));

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action); ;
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
