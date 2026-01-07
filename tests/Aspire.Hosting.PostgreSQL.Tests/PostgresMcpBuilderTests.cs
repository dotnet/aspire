// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Postgres;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.PostgreSQL.Tests;

[Experimental("ASPIREPOSTGRES001")]
public class PostgresMcpBuilderTests
{
    [Fact]
    public async Task WithPostgresMcpOnDatabaseAddsContainerResourceWithMcpEndpointAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPostgres("postgres")
            .AddDatabase("db")
            .WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var mcpContainer = Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());
        Assert.Equal("db-mcp", mcpContainer.Name);

        var endpoint = Assert.Single(mcpContainer.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(PostgresMcpContainerResource.PrimaryEndpointName, endpoint.Name);
        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(8000, endpoint.TargetPort);

        var mcpAnnotation = Assert.Single(mcpContainer.Annotations.OfType<McpServerEndpointAnnotation>());

        var resolvedUri = await mcpAnnotation.EndpointUrlResolver(mcpContainer, CancellationToken.None);

        Assert.NotNull(resolvedUri);
        Assert.Equal("http://db-mcp.dev.internal:8000/sse", resolvedUri!.ToString());
    }

    [Fact]
    public async Task WithPostgresMcpOnDatabaseSetsDatabaseUriEnvironmentVariable()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var pass = appBuilder.AddParameter("pass", "p@ssw0rd1");

        appBuilder.AddPostgres("postgres", password: pass)
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5432))
            .AddDatabase("db")
            .WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var database = Assert.Single(appModel.Resources.OfType<PostgresDatabaseResource>());
        var mcpContainer = Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());

        var env = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(mcpContainer, DistributedApplicationOperation.Run, TestServiceProvider.Instance);

        var databaseUri = Assert.Single(env, e => e.Key == "DATABASE_URI");
        Assert.Equal("postgresql://postgres:p%40ssw0rd1@postgres.dev.internal:5432/db", databaseUri.Value);
    }

    [Fact]
    public void WithPostgresMcpOnDatabaseCanBeCalledMultipleTimesWithoutDuplicateResources()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var db = appBuilder.AddPostgres("postgres")
            .AddDatabase("db");

        db.WithPostgresMcp();
        db.WithPostgresMcp();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Single(appModel.Resources.OfType<PostgresMcpContainerResource>());
    }

    [Fact]
    public void WithPostgresMcpWithCustomNameCreatesDistinctResourcesPerName()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var db = appBuilder.AddPostgres("postgres")
            .AddDatabase("db");

        db.WithPostgresMcp(containerName: "postgres-mcp-a");
        db.WithPostgresMcp(containerName: "postgres-mcp-b");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        Assert.Equal(2, appModel.Resources.OfType<PostgresMcpContainerResource>().Count());
    }
}
