// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AspireAzureOpenAIClientBuilderChatClientExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanReadDeploymentNameFromConfig(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("Aspire:Azure:AI:OpenAI:Endpoint", "https://aspireopenaitests.openai.azure.com/"),
            new("Aspire:Azure:AI:OpenAI:Deployment", "testdeployment1")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.GetService<ChatClientMetadata>()?.DefaultModelId);
    }

    [Theory]
    [InlineData(true, "Model")]
    [InlineData(false, "Model")]
    [InlineData(true, "Deployment")]
    [InlineData(false, "Deployment")]
    public void CanReadDeploymentNameFromConnectionString(bool useKeyed, string connectionStringKey)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;{connectionStringKey}=testdeployment1")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.GetService<ChatClientMetadata>()?.DefaultModelId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanAcceptDeploymentNameAsArgument(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient("testdeployment1");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.GetService<ChatClientMetadata>()?.DefaultModelId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RejectsConnectionStringWithBothModelAndDeployment(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;Deployment=testdeployment1;Model=something")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
                host.Services.GetRequiredService<IChatClient>();
        });

        Assert.StartsWith("The connection string 'openai' contains both 'Deployment' and 'Model' keys.", ex.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RejectsDeploymentNameNotSpecified(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient();
        }

        using var host = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
                host.Services.GetRequiredService<IChatClient>();
        });

        Assert.StartsWith("The deployment could not be determined", ex.Message);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void AddsOpenTelemetry(bool useKeyed, bool disableOpenTelemetry)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake"),
            new("Aspire:Azure:AI:OpenAI:DisableTracing", disableOpenTelemetry.ToString()),
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient("testdeployment1");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        Assert.Equal(disableOpenTelemetry, client.GetService<OpenTelemetryChatClient>() is null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanConfigurePipelineAsync(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedChatClient("openai_chatclient", "testdeployment1").Use(TestMiddleware, null);
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddChatClient("testdeployment1").Use(TestMiddleware, null);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IChatClient>("openai_chatclient") :
            host.Services.GetRequiredService<IChatClient>();

        var completion = await client.GetResponseAsync("Whatever", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("Hello from middleware", completion.Text);

        static Task<ChatResponse> TestMiddleware(IEnumerable<ChatMessage> list, ChatOptions? options, IChatClient client, CancellationToken token)
            => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Hello from middleware")));
    }
}
