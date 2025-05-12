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
            new("Aspire:Azure:AI:OpenAI:Endpoint", "https://aspireopenaitests.openai.azure.com/"),
            new("Aspire:Azure:AI:OpenAI:Deployment", "testdeployment1")
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
        var generator = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(generator);
        Assert.Equal("testdeployment1", generator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
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
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator();
        }

        using var host = builder.Build();
        var generator = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(generator);
        Assert.Equal("testdeployment1", generator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
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
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator", "testdeployment1");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator("testdeployment1");
        }

        using var host = builder.Build();
        var generator = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.NotNull(generator);
        Assert.Equal("testdeployment1", generator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId);
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
            new("ConnectionStrings:openai", $"Endpoint=https://aspireopenaitests.openai.azure.com/;Key=fake")
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
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator", "testdeployment1");
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator("testdeployment1");
        }

        using var host = builder.Build();
        var generator = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        Assert.Equal(disableOpenTelemetry, generator.GetService<OpenTelemetryEmbeddingGenerator<string, Embedding<float>>>() is null);
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
            builder.AddAzureOpenAIClient("openai").AddKeyedEmbeddingGenerator("openai_embeddinggenerator", "testdeployment1").Use(TestMiddleware);
        }
        else
        {
            builder.AddAzureOpenAIClient("openai").AddEmbeddingGenerator("testdeployment1").Use(TestMiddleware);
        }

        using var host = builder.Build();
        var generator = useKeyed ?
            host.Services.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>("openai_embeddinggenerator") :
            host.Services.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        var vector = await generator.GenerateVectorAsync("Hello");
        Assert.Equal(1.23f, vector.ToArray().Single());
    }

    private Task<GeneratedEmbeddings<Embedding<float>>> TestMiddleware(IEnumerable<string> inputs, EmbeddingGenerationOptions? options, IEmbeddingGenerator<string, Embedding<float>> nextAsync, CancellationToken cancellationToken)
    {
        float[] floats = [1.23f];
        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(inputs.Select(i => new Embedding<float>(floats))));
    }
}
