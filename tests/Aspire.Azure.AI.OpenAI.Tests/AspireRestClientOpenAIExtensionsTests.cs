// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AspireRestClientOpenAIExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyEndpointRegistersOpenAIComponent(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", "Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIRestApiClient("openai");
        }
        else
        {
            builder.AddOpenAIRestApiClient("openai");
        }

        using var host = builder.Build();
        var openAiClient = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<OpenAIClient>(openAiClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointRegistersAzureComponentIsAzureTrue(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", "Endpoint=https://aspireopenaitests.fake.com/;Key=fake;IsAzure=true")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIRestApiClient("openai");
        }
        else
        {
            builder.AddOpenAIRestApiClient("openai");
        }

        using var host = builder.Build();
        var openAiClient = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<AzureOpenAIClient>(openAiClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointRegistersOpenAIComponentIsAzureFalse(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", "Endpoint=https://aspireopenaitests.fake.com/;Key=fake;IsAzure=false")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIRestApiClient("openai");
        }
        else
        {
            builder.AddOpenAIRestApiClient("openai");
        }

        using var host = builder.Build();
        var openAiClient = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<OpenAIClient>(openAiClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointRegistersAzureComponentWithAzureDomain(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", "Endpoint=https://aspireopenaitests.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIRestApiClient("openai");
        }
        else
        {
            builder.AddOpenAIRestApiClient("openai");
        }

        using var host = builder.Build();
        var openAiClient = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<AzureOpenAIClient>(openAiClient);
    }

    [Theory]
    [InlineData("https://aspireopenaitests.azure.com/")]
    [InlineData("https://aspireopenaitests.AZURE.com/")]
    [InlineData("https://aspireopenaitests.azure.cn/")]
    public void EndpointRegistersAzureComponentWithValidAzureHosts(string domain)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint={domain};Key=fake")
        ]);

        builder.AddOpenAIRestApiClient("openai");
        
        using var host = builder.Build();
        var openAiClient = host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<AzureOpenAIClient>(openAiClient);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EndpointRegistersOpenAIWithAzureDomainIsAzureFalse(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", "Endpoint=https://aspireopenaitests.azure.com/;Key=fake;IsAzure=false")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedOpenAIRestApiClient("openai");
        }
        else
        {
            builder.AddOpenAIRestApiClient("openai");
        }

        using var host = builder.Build();
        var openAiClient = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.IsType<OpenAIClient>(openAiClient);
    }
}
