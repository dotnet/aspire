// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Postgres;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.PostgreSQL.Tests;

public class PostgresMcpBuilderTests
{
    [Fact]
    public void WithPostgresMcpOnServerAddsContainerResourceWithMcpEndpointAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPostgres("postgres")
            .WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mcpContainer = Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());
        Assert.Equal("postgres-mcp", mcpContainer.Name);

        var endpoint = Assert.Single(mcpContainer.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(PostgresMcpContainerResource.PrimaryEndpointName, endpoint.Name);
        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(8000, endpoint.TargetPort);

        var mcpAnnotation = Assert.Single(mcpContainer.Annotations.OfType<McpEndpointAnnotation>());
        Assert.Equal(PostgresMcpContainerResource.PrimaryEndpointName, mcpAnnotation.EndpointName);
        Assert.Equal("/sse", mcpAnnotation.Path);
    }

    [Fact]
    public async Task WithPostgresMcpOnServerSetsDatabaseUriEnvironmentVariable()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432))
            .WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var server = Assert.Single(appModel.Resources.OfType<PostgresServerResource>());
        var mcpContainer = Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mcpContainer, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var databaseUri = Assert.Single(env, e => e.Key == "DATABASE_URI");
        var uriProperty = Assert.Single(((IResourceWithConnectionString)server).GetConnectionProperties(), p => p.Key == "Uri");
        Assert.Equal(uriProperty.Value.ValueExpression, databaseUri.Value);
    }

    [Fact]
    public async Task WithPostgresMcpOnDatabaseSetsDatabaseUriEnvironmentVariable()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPostgres("postgres")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432))
            .AddDatabase("db")
            .WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var database = Assert.Single(appModel.Resources.OfType<PostgresDatabaseResource>());
        var mcpContainer = Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mcpContainer, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var databaseUri = Assert.Single(env, e => e.Key == "DATABASE_URI");
        var uriProperty = Assert.Single(((IResourceWithConnectionString)database).GetConnectionProperties(), p => p.Key == "Uri");
        Assert.Equal(uriProperty.Value.ValueExpression, databaseUri.Value);
    }

    [Fact]
    public void WithPostgresMcpCanBeCalledMultipleTimesWithoutDuplicateResources()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var postgres = appBuilder.AddPostgres("postgres");

        postgres.WithPostgresMcp();
        postgres.WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());
    }

    [Fact]
    public void WithPostgresMcpWithCustomNameCreatesDistinctResourcesPerName()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var postgres = appBuilder.AddPostgres("postgres");

        postgres.WithPostgresMcp(containerName: "postgres-mcp-a");
        postgres.WithPostgresMcp(containerName: "postgres-mcp-b");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Equal(2, appModel.Resources.OfType<PostgresMcpContainerResource>().Count());
    }
}
