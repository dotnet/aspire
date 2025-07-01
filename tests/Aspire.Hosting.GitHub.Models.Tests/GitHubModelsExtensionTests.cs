// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.GitHub.Models.Tests;

public class GitHubModelsExtensionTests
{
    [Fact]
    public void AddGitHubModelAddsResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.Equal("github", github.Resource.Name);
        Assert.Equal("openai/gpt-4o-mini", github.Resource.Model);
        Assert.Equal(GitHubModelsResource.DefaultEndpoint, github.Resource.Endpoint);
    }

    [Fact]
    public void WithEndpointSetsEndpointCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var customEndpoint = "https://custom.endpoint.com";
        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini")
                           .WithEndpoint(customEndpoint);

        Assert.Equal(customEndpoint, github.Resource.Endpoint);
    }

    [Fact]
    public void WithApiKeySetsKeyCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var apiKey = "test-api-key";
        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini")
                           .WithApiKey(apiKey);

        Assert.Equal(apiKey, github.Resource.Key);
    }

    [Fact]
    public void ConnectionStringExpressionIsCorrectlyFormatted()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini")
                           .WithApiKey("test-key");

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
        Assert.Contains("Key=test-key", connectionString);
        Assert.Contains("Model=openai/gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=openai/gpt-4o-mini", connectionString);
    }

    [Fact]
    public void WithApiKeySetFromParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var randomApiKey = $"test-key";
        var apiKeyParameter = builder.AddParameter("github-api-key", secret: true);
        builder.Configuration["Parameters:github-api-key"] = randomApiKey;

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini")
                           .WithApiKey(apiKeyParameter);

        Assert.Equal(randomApiKey, github.Resource.Key);
    }
}
