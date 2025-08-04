// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.OpenAI.Tests;

public class OpenAIExtensionTests
{
    [Fact]
    public void AddOpenAIModelAddsResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        Assert.Equal("openai", openai.Resource.Name);
        Assert.Equal("gpt-4o-mini", openai.Resource.Model);
    }

    [Fact]
    public void AddOpenAIModelCreatesDefaultApiKeyParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var openai = builder.AddOpenAIModel("mymodel", "gpt-4o-mini");

        // Verify that the API key parameter exists and follows the naming pattern
        Assert.NotNull(openai.Resource.Key);
        Assert.Equal("mymodel-openai-apikey", openai.Resource.Key.Name);
        Assert.True(openai.Resource.Key.Secret);
    }

    [Fact]
    public void AddOpenAIModelUsesCorrectEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        var connectionString = openai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Endpoint=https://api.openai.com/v1", connectionString);
    }

    [Fact]
    public void ConnectionStringExpressionIsCorrectlyFormatted()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        var connectionString = openai.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Endpoint=https://api.openai.com/v1", connectionString);
        Assert.Contains("Model=gpt-4o-mini", connectionString);
        Assert.Contains("Key=", connectionString);
    }

    [Fact]
    public async Task WithApiKeySetFromParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        const string apiKey = "sk-test-key";

        var apiKeyParameter = builder.AddParameter("openai-api-key", secret: true);
        builder.Configuration["Parameters:openai-api-key"] = apiKey;

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini")
                           .WithApiKey(apiKeyParameter);

        var connectionString = await openai.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(apiKeyParameter.Resource, openai.Resource.Key);
        Assert.Contains($"Key={apiKey}", connectionString);
    }

    [Fact]
    public void DefaultKeyParameterIsCreated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        Assert.NotNull(openai.Resource.Key);
        Assert.Equal("openai-openai-apikey", openai.Resource.Key.Name);
        Assert.True(openai.Resource.Key.Secret);
    }

    [Fact]
    public void OpenAIModelResourceConstructor()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var apiKeyParameter = builder.AddParameter("openai-api-key", secret: true);
        builder.Configuration["Parameters:openai-api-key"] = "test-key";

        var resource = new OpenAIModelResource("test", "gpt-4o-mini", apiKeyParameter.Resource);

        Assert.Equal("test", resource.Name);
        Assert.Equal("gpt-4o-mini", resource.Model);
        Assert.Equal(apiKeyParameter.Resource, resource.Key);
    }

    [Fact]
    public void WithApiKeyThrowsIfParameterIsNotSecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");
        var apiKey = builder.AddParameter("non-secret-key"); // Not marked as secret

        var exception = Assert.Throws<ArgumentException>(() => openai.WithApiKey(apiKey));
        Assert.Contains("The API key parameter must be marked as secret", exception.Message);
    }

    [Fact]
    public void WithApiKeySucceedsIfParameterIsSecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");
        var apiKey = builder.AddParameter("secret-key", secret: true);

        // This should not throw
        var result = openai.WithApiKey(apiKey);
        Assert.NotNull(result);
        Assert.Equal(apiKey.Resource, openai.Resource.Key);
    }

    [Fact]
    public void WithApiKeyCalledTwiceOnlyRemovesDefaultParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "openai-openai-apikey"));

        openai.WithApiKey(builder.AddParameter("secret-key1", secret: true));

        Assert.Null(builder.Resources.FirstOrDefault(r => r.Name == "openai-openai-apikey"));
        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key1"));

        openai.WithApiKey(builder.AddParameter("secret-key2", secret: true));

        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key1"));
        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key2"));
    }

    [Fact]
    public void WithHealthCheckAddsHealthCheckAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini").WithHealthCheck();

        // Verify that the health check annotation is added
        var healthCheckAnnotations = openai.Resource.Annotations.OfType<HealthCheckAnnotation>().ToList();
        Assert.Single(healthCheckAnnotations);
        Assert.Equal("openai_check", healthCheckAnnotations[0].Key);
    }

    [Fact]
    public void WithHealthCheckEnsuresIHttpClientFactoryIsRegistered()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        // Add the health check without explicitly calling AddHttpClient
        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini").WithHealthCheck();

        // Build the service provider to test dependency resolution
        var services = builder.Services.BuildServiceProvider();

        // This should not throw because WithHealthCheck should ensure IHttpClientFactory is registered
        var httpClientFactory = services.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void WithHealthCheckWorksWhenAddHttpClientIsCalledManually()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "test-api-key";

        // Manually call AddHttpClient (should not conflict with automatic registration in WithHealthCheck)
        builder.Services.AddHttpClient();

        // Add the health check
        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini").WithHealthCheck();

        // Build the service provider to test dependency resolution
        var services = builder.Services.BuildServiceProvider();

        // This should work fine since AddHttpClient can be called multiple times safely
        var httpClientFactory = services.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public async Task ConnectionStringExpressionResolves()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:openai-openai-apikey"] = "sk-test123";

        var openai = builder.AddOpenAIModel("openai", "gpt-4o-mini");

        var connectionString = await openai.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Contains("Endpoint=https://api.openai.com/v1", connectionString);
        Assert.Contains("Key=sk-test123", connectionString);
        Assert.Contains("Model=gpt-4o-mini", connectionString);
    }

    [Fact]
    public void AddOpenAIModelThrowsIfBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            OpenAIExtensions.AddOpenAIModel(null!, "test", "gpt-4o-mini"));
    }

    [Fact]
    public void AddOpenAIModelThrowsIfNameIsNullOrEmpty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.ThrowsAny<ArgumentException>(() => 
            builder.AddOpenAIModel("", "gpt-4o-mini"));
        
        Assert.ThrowsAny<ArgumentException>(() => 
            builder.AddOpenAIModel(null!, "gpt-4o-mini"));
    }

    [Fact]
    public void AddOpenAIModelThrowsIfModelIsNullOrEmpty()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        Assert.ThrowsAny<ArgumentException>(() => 
            builder.AddOpenAIModel("test", ""));
        
        Assert.ThrowsAny<ArgumentException>(() => 
            builder.AddOpenAIModel("test", null!));
    }

    [Fact]
    public void WithApiKeyThrowsIfBuilderIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var apiKey = builder.AddParameter("test", secret: true);

        Assert.Throws<ArgumentNullException>(() => 
            OpenAIExtensions.WithApiKey(null!, apiKey));
    }

    [Fact]
    public void WithApiKeyThrowsIfApiKeyIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var openai = builder.AddOpenAIModel("test", "gpt-4o-mini");

        Assert.Throws<ArgumentNullException>(() => 
            openai.WithApiKey(null!));
    }

    [Fact]
    public void WithHealthCheckThrowsIfBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => 
            OpenAIExtensions.WithHealthCheck(null!));
    }

    [Theory]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("text-embedding-3-small")]
    [InlineData("dall-e-3")]
    public void AddOpenAIModelWorksWithDifferentModels(string modelName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration[$"Parameters:test-openai-apikey"] = "test-key";

        var openai = builder.AddOpenAIModel("test", modelName);

        Assert.Equal("test", openai.Resource.Name);
        Assert.Equal(modelName, openai.Resource.Model);
        
        var connectionString = openai.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains($"Model={modelName}", connectionString);
    }
}
