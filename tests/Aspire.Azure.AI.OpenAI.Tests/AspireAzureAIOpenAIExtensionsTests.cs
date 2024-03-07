// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AspireAzureAIOpenAIExtensionsTests
{
    private const string ConnectionString = "Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureOpenAIClient("openai");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var uri = new Uri("https://aspireopenaitests.openai.azure.com/");
        var key = "fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedAzureOpenAIClient("openai", settings => { settings.Endpoint = uri; settings.Key = key; });
        }
        else
        {
            builder.AddAzureOpenAIClient("openai", settings => { settings.Endpoint = uri; settings.Key = key; });
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<OpenAIClient>("openai") :
            host.Services.GetRequiredService<OpenAIClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("https://yourservice.openai.azure.com/")]
    [InlineData("http://domain:12345")]
    [InlineData("Endpoint=http://domain.com:12345;Key=abc123")]
    [InlineData("Endpoint=http://domain.com:12345")]
    public void ReadsFromConnectionStringsFormats(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", connectionString)
        ]);

        builder.AddAzureOpenAIClient("openai");

        var host = builder.Build();
        var client = host.Services.GetRequiredService<OpenAIClient>();

        Assert.NotNull(client);
    }

}
