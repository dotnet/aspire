// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSqlExtensionsTests()
{
    [Theory]
    // [InlineData(true, true)] this scenario is covered in RoleAssignmentTests.SqlSupport. The output doesn't match the pattern here because the role assignment isn't generated
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task AddAzureSqlServer(bool publishMode, bool useAcaInfrastructure)
    {
        using var builder = TestDistributedApplicationBuilder.Create(publishMode ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run);

        var sql = builder.AddAzureSqlServer("sql");

        sql.AddDatabase("db1");
        sql.AddDatabase("db2", "db2Name");

        if (useAcaInfrastructure)
        {
            builder.AddAzureContainerAppEnvironment("env");

            // on ACA infrastructure, if there are no references to the resource,
            // then there won't be any roles created. So add a reference here.
            builder.AddContainer("api", "myimage")
                .WithReference(sql);
        }

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(sql.Resource, skipPreparer: true);

        var expectedManifest = $$"""
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sql.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        await Verifier.Verify(manifest.BicepText, extension: "bicep")
            .UseHelixAwareDirectory();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureSqlServerRunAsContainerProducesCorrectConnectionString(bool addDbBeforeRunAsContainer)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var sql = builder.AddAzureSqlServer("sql");

        IResourceBuilder<AzureSqlDatabaseResource> db1 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db2 = null!;
        if (addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");

        }
        sql.RunAsContainer(c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
        });

        if (!addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
        }

        Assert.True(sql.Resource.IsContainer(), "The resource should now be a container resource.");
        var serverConnectionString = await sql.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", serverConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true", serverConnectionString);

        var db1ConnectionString = await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db1ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db1", db1ConnectionString);

        var db2ConnectionString = await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db2ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db2Name", db2ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzureSqlServerRunAsContainerProducesCorrectPasswordAndPort(bool addDbBeforeRunAsContainer)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var sql = builder.AddAzureSqlServer("sql");
        var pass = builder.AddParameter("pass", "p@ssw0rd1");
        IResourceBuilder<AzureSqlDatabaseResource> db1 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db2 = null!;
        if (addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
        }

        IResourceBuilder<SqlServerServerResource>? innerSql = null;
        sql.RunAsContainer(configureContainer: c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
                .WithHostPort(12455)
                .WithPassword(pass);
            innerSql = c;
        });

        Assert.NotNull(innerSql);

        if (!addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
        }

        var endpoint = Assert.Single(innerSql.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1433, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(12455, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        Assert.True(sql.Resource.IsContainer(), "The resource should now be a container resource.");
        var serverConnectionString = await sql.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.Equal("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true", serverConnectionString);

        var db1ConnectionString = await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=db1", db1ConnectionString);

        var db2ConnectionString = await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=db2Name", db2ConnectionString);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void RunAsContainerAppliesAnnotationsCorrectly(bool annotationsBefore, bool addDatabaseBefore)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var sql = builder.AddAzureSqlServer("sql");
        IResourceBuilder<AzureSqlDatabaseResource>? db = null;

        if (addDatabaseBefore)
        {
            db = sql.AddDatabase("db1");
        }

        if (annotationsBefore)
        {
            sql.WithAnnotation(new Dummy1Annotation());
            db?.WithAnnotation(new Dummy1Annotation());
        }

        sql.RunAsContainer(c =>
        {
            c.WithAnnotation(new Dummy2Annotation());
        });

        if (!addDatabaseBefore)
        {
            db = sql.AddDatabase("db1");

            if (annotationsBefore)
            {
                // need to add the annotation here in this case becuase it has to be added after the DB is created
                db!.WithAnnotation(new Dummy1Annotation());
            }
        }

        if (!annotationsBefore)
        {
            sql.WithAnnotation(new Dummy1Annotation());
            db!.WithAnnotation(new Dummy1Annotation());
        }

        var sqlResourceInModel = builder.Resources.Single(r => r.Name == "sql");
        var dbResourceInModel = builder.Resources.Single(r => r.Name == "db1");

        Assert.True(sqlResourceInModel.TryGetAnnotationsOfType<Dummy1Annotation>(out var sqlAnnotations1));
        Assert.Single(sqlAnnotations1);

        Assert.True(sqlResourceInModel.TryGetAnnotationsOfType<Dummy2Annotation>(out var sqlAnnotations2));
        Assert.Single(sqlAnnotations2);

        Assert.True(dbResourceInModel.TryGetAnnotationsOfType<Dummy1Annotation>(out var dbAnnotations));
        Assert.Single(dbAnnotations);
    }

    private sealed class Dummy1Annotation : IResourceAnnotation
    {
    }

    private sealed class Dummy2Annotation : IResourceAnnotation
    {
    }
}
