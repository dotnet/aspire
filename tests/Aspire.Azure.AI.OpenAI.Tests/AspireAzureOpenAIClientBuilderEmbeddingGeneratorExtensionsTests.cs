// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.AI.OpenAI.Tests;

public class AspireAzureOpenAIClientBuilderEmbeddingGeneratorExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanReadDeploymentNameFromConfig(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Azure:AI:OpenAI:Endpoint", "https://aspireopenaitests.openai.azure.com/"),
            new KeyValuePair<string, string?>("Aspire:Azure:AI:OpenAI:Deployment", "testdeployment1")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.Metadata.ModelId);
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
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;{connectionStringKey}=testdeployment1")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator();
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.Metadata.ModelId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanAcceptDeploymentNameAsArgument(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator", "testdeployment1");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator("testdeployment1");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(client);
        Assert.Equal("testdeployment1", client.Metadata.ModelId);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RejectsConnectionStringWithBothModelAndDeployment(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake;Deployment=testdeployment1;Model=something")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator();
        }

        using var host = builder.Build();
        
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
                host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
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
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator();
        }

        using var host = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = useKeyed ?
                host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
                host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        });

        Assert.StartsWith($"An {nameof(IEmbeddingGenerator<string, Embedding<float>>)} could not be configured", ex.Message);
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
            new KeyValuePair<string, string?>("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
        ]);

        if (useKeyed)
        {
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_chatclient", "testdeployment1", disableOpenTelemetry);
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator("testdeployment1", disableOpenTelemetry);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_chatclient") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.Equal(disableOpenTelemetry, client.GetService<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>() is null);
    }
}