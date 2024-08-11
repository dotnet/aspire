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
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureOpenAIClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddAzureOpenAIClientShouldThrowWhenConnectionNameIsNull()
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        string connectionName = null!;
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureOpenAIClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddAzureOpenAIClientShouldThrowWhenConnectionNameIsEmpty()
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var connectionName = string.Empty;
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddAzureOpenAIClient(connectionName, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(connectionName), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenBuilderIsNull()
    {
        IHostApplicationBuilder builder = null!;
        const string name = "openai";
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureOpenAIClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenNameIsNull()
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        string name = null!;
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureOpenAIClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddKeyedAzureOpenAIClientShouldThrowWhenNameIsEmpty()
    {
        IHostApplicationBuilder builder = new HostApplicationBuilder();
        var name = string.Empty;
        Action<AzureOpenAISettings>? configureSettings = null;
        Action<IAzureClientBuilder<AzureOpenAIClient, AzureOpenAIClientOptions>>? configureClientBuilder = null;

        var action = () => builder.AddKeyedAzureOpenAIClient(name, configureSettings, configureClientBuilder);

        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
