// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

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

    [Fact]
    public void AddGitHubModelWithoutOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.Null(github.Resource.Organization);
    }

    [Fact]
    public void AddGitHubModelWithOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini", orgParameter);

        Assert.NotNull(github.Resource.Organization);
        Assert.Equal("github-org", github.Resource.Organization.Name);
        Assert.Equal(orgParameter.Resource, github.Resource.Organization);
    }

    [Fact]
    public void ConnectionStringExpressionWithOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini", orgParameter);

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Endpoint=https://models.github.ai/orgs/", connectionString);
        Assert.Contains("/inference", connectionString);
        Assert.Contains("Model=openai/gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=openai/gpt-4o-mini", connectionString);
        Assert.Contains("Key=", connectionString);
    }

    [Fact]
    public async Task ConnectionStringExpressionWithOrganizationResolvesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini", orgParameter);

        var connectionString = await github.Resource.ConnectionStringExpression.GetValueAsync(default);

        Assert.Contains("Endpoint=https://models.github.ai/orgs/myorg/inference", connectionString);
        Assert.Contains("Model=openai/gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=openai/gpt-4o-mini", connectionString);
    }

    [Fact]
    public void ConnectionStringExpressionWithoutOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
        Assert.DoesNotContain("/orgs/", connectionString);
        Assert.Contains("Model=openai/gpt-4o-mini", connectionString);
        Assert.Contains("DeploymentId=openai/gpt-4o-mini", connectionString);
    }

    [Fact]
    public void GitHubModelResourceConstructorSetsOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", orgParameter.Resource);

        Assert.Equal("test", resource.Name);
        Assert.Equal("openai/gpt-4o-mini", resource.Model);
        Assert.Equal(orgParameter.Resource, resource.Organization);
    }

    [Fact]
    public void GitHubModelResourceConstructorWithNullOrganization()
    {
        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", null);

        Assert.Equal("test", resource.Name);
        Assert.Equal("openai/gpt-4o-mini", resource.Model);
        Assert.Null(resource.Organization);
    }

    [Fact]
    public void GitHubModelResourceOrganizationCanBeChanged()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", null);
        Assert.Null(resource.Organization);

        resource.Organization = orgParameter.Resource;
        Assert.Equal(orgParameter.Resource, resource.Organization);
    }

    [Fact]
    public void WithHealthCheckAddsHealthCheckAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini").WithHealthCheck();

        // Verify that the health check annotation is added
        var healthCheckAnnotations = github.Resource.Annotations.OfType<HealthCheckAnnotation>().ToList();
        Assert.Single(healthCheckAnnotations);
        Assert.Equal("github_check", healthCheckAnnotations[0].Key);
    }
}
