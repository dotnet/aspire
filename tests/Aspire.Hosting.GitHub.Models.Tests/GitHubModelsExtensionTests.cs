// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.GitHub.Models.Tests;

public class GitHubModelsExtensionTests
{
    [Fact]
    public void AddGitHubModelAddsResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var github = builder.AddGitHubModel("github", "gpt-4o-mini");
        
        Assert.Equal("github", github.Resource.Name);
        Assert.Equal("gpt-4o-mini", github.Resource.Model);
        Assert.Equal(GitHubModelsResource.DefaultEndpoint, github.Resource.Endpoint);
    }

    [Fact]
    public void WithEndpointSetsEndpointCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var customEndpoint = "https://custom.endpoint.com";
        var github = builder.AddGitHubModel("github", "gpt-4o-mini")
                           .WithEndpoint(customEndpoint);
        
        Assert.Equal(customEndpoint, github.Resource.Endpoint);
    }

    [Fact]
    public void WithApiKeySetsKeyCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var apiKey = "test-api-key";
        var github = builder.AddGitHubModel("github", "gpt-4o-mini")
                           .WithApiKey(apiKey);
        
        Assert.Equal(apiKey, github.Resource.Key);
    }

    [Fact]
    public void ConnectionStringExpressionIsCorrectlyFormatted()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var github = builder.AddGitHubModel("github", "gpt-4o-mini")
                           .WithApiKey("test-key");
        
        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;
        
        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
        Assert.Contains("Key=test-key", connectionString);
        Assert.Contains("Model=gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=gpt-4o-mini", connectionString);
    }

    [Fact]
    public void WithApiKeyParameterAddsEnvironmentAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        
        var apiKeyParam = builder.AddParameter("github-api-key", secret: true);
        var github = builder.AddGitHubModel("github", "gpt-4o-mini")
                           .WithApiKey(apiKeyParam);
        
        var annotations = github.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.True(annotations.Any(), "Expected EnvironmentCallbackAnnotation to be added");
    }
}
