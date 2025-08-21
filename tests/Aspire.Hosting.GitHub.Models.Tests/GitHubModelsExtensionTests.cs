// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.GitHub.Models.Tests;

public class GitHubModelsExtensionTests
{
    [Fact]
    public void AddGitHubModelAddsResourceWithCorrectName()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.Equal("github", github.Resource.Name);
        Assert.Equal("openai/gpt-4o-mini", github.Resource.Model);
    }

    [Fact]
    public void AddGitHubModelCreatesDefaultApiKeyParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("mymodel", "openai/gpt-4o-mini");

        // Verify that the API key parameter exists and follows the naming pattern
        Assert.NotNull(github.Resource.Key);
        Assert.Equal("mymodel-gh-apikey", github.Resource.Key.Name);
        Assert.True(github.Resource.Key.Secret);
    }

    [Fact]
    public void AddGitHubModelUsesCorrectEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        var connectionString = github.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Endpoint=https://models.github.ai/inference", connectionString);
    }

    [Fact]
    public void ConnectionStringExpressionIsCorrectlyFormatted()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

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
        Assert.Equal("github-gh-apikey", github.Resource.Key.Name);
        Assert.True(github.Resource.Key.Secret);
    }

    [Fact]
    public void AddGitHubModelWithoutOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.Null(github.Resource.Organization);
    }

    [Fact]
    public void AddGitHubModelWithOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

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
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

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
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

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
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

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

        var apiKeyParameter = builder.AddParameter("github-api-key", secret: true);
        builder.Configuration["Parameters:github-api-key"] = "test-key";

        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", orgParameter.Resource, apiKeyParameter.Resource);

        Assert.Equal("test", resource.Name);
        Assert.Equal("openai/gpt-4o-mini", resource.Model);
        Assert.Equal(orgParameter.Resource, resource.Organization);
        Assert.Equal(apiKeyParameter.Resource, resource.Key);
    }

    [Fact]
    public void GitHubModelResourceConstructorWithNullOrganization()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var apiKeyParameter = builder.AddParameter("github-api-key", secret: true);
        builder.Configuration["Parameters:github-api-key"] = "test-key";

        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", null, apiKeyParameter.Resource);

        Assert.Equal("test", resource.Name);
        Assert.Equal("openai/gpt-4o-mini", resource.Model);
        Assert.Null(resource.Organization);
        Assert.Equal(apiKeyParameter.Resource, resource.Key);
    }

    [Fact]
    public void GitHubModelResourceOrganizationCanBeChanged()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var orgParameter = builder.AddParameter("github-org");
        builder.Configuration["Parameters:github-org"] = "myorg";

        var apiKeyParameter = builder.AddParameter("github-api-key", secret: true);
        builder.Configuration["Parameters:github-api-key"] = "test-key";

        var resource = new GitHubModelResource("test", "openai/gpt-4o-mini", null, apiKeyParameter.Resource);
        Assert.Null(resource.Organization);

        resource.Organization = orgParameter.Resource;
        Assert.Equal(orgParameter.Resource, resource.Organization);
    }

    [Fact]
    public void WithApiKeyThrowsIfParameterIsNotSecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");
        var apiKey = builder.AddParameter("non-secret-key"); // Not marked as secret

        var exception = Assert.Throws<ArgumentException>(() => github.WithApiKey(apiKey));
        Assert.Contains("The API key parameter must be marked as secret", exception.Message);
    }

    [Fact]
    public void WithApiKeySucceedsIfParameterIsSecret()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");
        var apiKey = builder.AddParameter("secret-key", secret: true);

        // This should not throw
        var result = github.WithApiKey(apiKey);
        Assert.NotNull(result);
        Assert.Equal(apiKey.Resource, github.Resource.Key);
    }

    [Fact]
    public void WithApiKeyCalledTwiceOnlyRemovesDefaultParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini");

        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "github-gh-apikey"));

        github.WithApiKey(builder.AddParameter("secret-key1", secret: true));

        Assert.Null(builder.Resources.FirstOrDefault(r => r.Name == "github-gh-apikey"));
        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key1"));

        github.WithApiKey(builder.AddParameter("secret-key2", secret: true));

        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key1"));
        Assert.NotNull(builder.Resources.FirstOrDefault(r => r.Name == "secret-key2"));
    }

    [Fact]
    public void WithHealthCheckAddsHealthCheckAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini").WithHealthCheck();

        // Verify that the health check annotation is added
        var healthCheckAnnotations = github.Resource.Annotations.OfType<HealthCheckAnnotation>().ToList();
        Assert.Single(healthCheckAnnotations);
        Assert.Equal("github_check", healthCheckAnnotations[0].Key);
    }

    [Fact]
    public void WithHealthCheckEnsuresIHttpClientFactoryIsRegistered()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        // Add the health check without explicitly calling AddHttpClient
        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini").WithHealthCheck();

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
        builder.Configuration["Parameters:github-gh-apikey"] = "test-api-key";

        // Manually call AddHttpClient (should not conflict with automatic registration in WithHealthCheck)
        builder.Services.AddHttpClient();

        // Add the health check
        var github = builder.AddGitHubModel("github", "openai/gpt-4o-mini").WithHealthCheck();

        // Build the service provider to test dependency resolution
        var services = builder.Services.BuildServiceProvider();

        // This should work fine since AddHttpClient can be called multiple times safely
        var httpClientFactory = services.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }
}
