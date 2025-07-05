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
    }

    [Fact]
    public void AddGitHubModelUsesCorrectEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
    }

    [Fact]
    public void ConnectionStringExpressionIsCorrectlyFormatted()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
        Assert.Contains("Model=openai/gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=openai/gpt-4o-mini", connectionString);
        Assert.Contains("Key=", connectionString);
    }

    [Fact]
    public async Task WithApiKeySetFromParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        const string apiKey = "randomkey";

        var apiKeyParameter = builder.AddParameter("github-api-key", secret: true);
        builder.Configuration["Parameters:github-api-key"] = apiKey;

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini")
                           .WithApiKey(apiKeyParameter);

        var connectionString = await github.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Equal(apiKeyParameter.Resource, github.Resource.Key);
        Assert.Contains($"Key={apiKey}", connectionString);
    }

    [Fact]
    public void DefaultKeyParameterIsCreated()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.NotNull(github.Resource.Key);
        Assert.Equal("github-api-key", github.Resource.Key.Name);
        Assert.True(github.Resource.Key.Secret);
    }
}
