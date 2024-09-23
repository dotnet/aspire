// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Tests;

public class ResourceExtensionsTests
{
    [Fact]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "latest").WithImageSHA256("1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991");

        Assert.True(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.Equal("grafana/grafana@sha256:1adbcc2df3866ff5ec1d836e9d2220c904c7f98901b918d3cc5e1118ab1af991", imageName);
    }

    [Fact]
    public void TryGetContainerImageNameReturnsCorrectFormatWhenShaNotSupplied()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("grafana", "grafana/grafana", "10.3.1");

        Assert.True(container.Resource.TryGetContainerImageName(out var imageName));
        Assert.Equal("grafana/grafana:10.3.1", imageName);
    }

    [Fact]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesInRunMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment(context =>
         {
             context.EnvironmentVariables["ELASTIC_PASSWORD"] = "p@ssw0rd1";
         });

        var env = await container.Resource.GetEnvironmentVariableValuesAsync();

        Assert.Collection(env,
            env =>
            {
                Assert.Equal("discovery.type", env.Key);
                Assert.Equal("single-node", env.Value);
            },
            env =>
            {
                Assert.Equal("xpack.security.enabled", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("ELASTIC_PASSWORD", env.Key);
                Assert.Equal("p@ssw0rd1", env.Value);
            });
    }

    [Fact]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesUsingValueProviderInRunMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:ElasticPassword"] = "p@ssw0rd1";

        var passwordParameter = builder.AddParameter("ElasticPassword");

        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment("ELASTIC_PASSWORD", passwordParameter);

        var env = await container.Resource.GetEnvironmentVariableValuesAsync();

        Assert.Collection(env,
            env =>
            {
                Assert.Equal("discovery.type", env.Key);
                Assert.Equal("single-node", env.Value);
            },
            env =>
            {
                Assert.Equal("xpack.security.enabled", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("ELASTIC_PASSWORD", env.Key);
                Assert.Equal("p@ssw0rd1", env.Value);
            });
    }

    [Fact]
    public async Task GetEnvironmentVariableValuesAsyncReturnCorrectVariablesUsingManifestExpressionProviderInPublishMode()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.Configuration["Parameters:ElasticPassword"] = "p@ssw0rd1";

        var passwordParameter = builder.AddParameter("ElasticPassword");

        var container = builder.AddContainer("elasticsearch", "library/elasticsearch", "8.14.0")
         .WithEnvironment("discovery.type", "single-node")
         .WithEnvironment("xpack.security.enabled", "true")
         .WithEnvironment("ELASTIC_PASSWORD", passwordParameter);

        var env = await container.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);

        Assert.Collection(env,
            env =>
            {
                Assert.Equal("discovery.type", env.Key);
                Assert.Equal("single-node", env.Value);
            },
            env =>
            {
                Assert.Equal("xpack.security.enabled", env.Key);
                Assert.Equal("true", env.Value);
            },
            env =>
            {
                Assert.Equal("{ElasticPassword.value}", env.Value);
                Assert.False(string.IsNullOrEmpty(env.Value));
            });
    }
}
