// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.Inference.Tests;

public class AspireAzureAIInferenceExtensionTests
{
    private const string ConnectionString = "Endpoint=https://fakeendpoint;Key=fakekey;DeploymentId=deployment";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", ConnectionString)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedChatCompletionsClient("inference");
        }
        else
        {
            builder.AddChatCompletionsClient("inference");
        }
        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<ChatCompletionsClient>("inference") :
            host.Services.GetService<ChatCompletionsClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", "Endpoint=unused;Key=myAccount;DeploymentId=unused")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedChatCompletionsClient("inference", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddChatCompletionsClient("inference", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();

        var client = useKeyed ?
            host.Services.GetKeyedService<ChatCompletionsClient>("inference") :
            host.Services.GetService<ChatCompletionsClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:inference2", ConnectionString + "2")
        ]);
        builder.AddKeyedChatCompletionsClient("inference1");
        builder.AddKeyedChatCompletionsClient("inference2");
        using var host = builder.Build();
        var client1 = host.Services.GetKeyedService<ChatCompletionsClient>("inference1");
        var client2 = host.Services.GetKeyedService<ChatCompletionsClient>("inference2");
        Assert.NotNull(client1);
        Assert.NotNull(client2);

        Assert.NotSame(client1, client2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRegisterAsAnIChatClient(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", ConnectionString)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedChatCompletionsClient("inference").AddKeyedChatClient("inference");
        }
        else
        {
            builder.AddChatCompletionsClient("inference").AddChatClient();
        }
        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<IChatClient>("inference") :
            host.Services.GetService<IChatClient>();
        Assert.NotNull(client);
    }
}
