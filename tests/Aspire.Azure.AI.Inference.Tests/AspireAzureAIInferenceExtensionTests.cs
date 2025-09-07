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
            builder.AddKeyedAzureChatCompletionsClient("inference");
        }
        else
        {
            builder.AddAzureChatCompletionsClient("inference");
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
            new KeyValuePair<string, string?>("ConnectionStrings:inference", "Endpoint=https://endpoint;Key=myAccount;DeploymentId=unused")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureChatCompletionsClient("inference", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureChatCompletionsClient("inference", settings => settings.ConnectionString = ConnectionString);
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
        builder.AddKeyedAzureChatCompletionsClient("inference1");
        builder.AddKeyedAzureChatCompletionsClient("inference2");
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
            builder.AddKeyedAzureChatCompletionsClient("inference").AddKeyedChatClient("inference");
        }
        else
        {
            builder.AddAzureChatCompletionsClient("inference").AddChatClient();
        }
        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<IChatClient>("inference") :
            host.Services.GetService<IChatClient>();
        Assert.NotNull(client);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddChatClientUsesCustomDeploymentId(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", ConnectionString)
        ]);
        if (useKeyed)
        {
            builder.AddKeyedAzureChatCompletionsClient("inference").AddKeyedChatClient("inference", deploymentName: "other");
        }
        else
        {
            builder.AddAzureChatCompletionsClient("inference").AddChatClient(deploymentName: "other");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetKeyedService<IChatClient>("inference") :
            host.Services.GetService<IChatClient>();

        var metadata = client?.GetService<ChatClientMetadata>();

        Assert.NotNull(metadata);
        Assert.Equal("other", metadata?.DefaultModelId);
    }

    [Theory]
    [InlineData("Deployment")]
    [InlineData("DeploymentId")]
    [InlineData("Model")]
    public void ChatCompletionsClientSettings_AcceptsSingleDeploymentKey(string keyName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionString = $"Endpoint=https://fakeendpoint;Key=fakekey;{keyName}=testdeployment";
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", connectionString)
        ]);

        builder.AddAzureChatCompletionsClient("inference");

        using var host = builder.Build();
        var client = host.Services.GetService<ChatCompletionsClient>();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("Deployment", "DeploymentId")]
    [InlineData("Deployment", "Model")]
    [InlineData("DeploymentId", "Model")]
    public void ChatCompletionsClientSettings_RejectsMultipleDeploymentKeys(string key1, string key2)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionString = $"Endpoint=https://fakeendpoint;Key=fakekey;{key1}=value1;{key2}=value2";
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:inference", connectionString)
        ]);

        // The exception should be thrown during this call
        var ex = Assert.Throws<ArgumentException>(() => builder.AddAzureChatCompletionsClient("inference"));
        Assert.Contains("multiple deployment/model keys", ex.Message);
        Assert.Contains(key1, ex.Message);
        Assert.Contains(key2, ex.Message);
    }
}
