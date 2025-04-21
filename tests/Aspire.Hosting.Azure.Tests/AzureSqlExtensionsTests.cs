// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSqlExtensionsTests(ITestOutputHelper output)
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

        // database name same as the aspire resource name, free tier 
        sql.AddDatabase("db1");

        // set the database name, free tier 
        sql.AddDatabase("db2", "db2Name");

        // set the database name, set the sku to HS_Gen5_2
        sql.AddDatabase("db3", "db3Name").WithSku("HS_Gen5_2");

        // set the database name, set the sku to Basic
        sql.AddDatabase("db4", "db4Name", "Basic");

        // set the database name, explicitly ask for the free tier
        // (which does not exist in reality, but we using the "Free" moniker
        // to indicate that we want to take advantage of the free offer
        sql.AddDatabase("db5", "db5Name").WithSku("Free");

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

        var principalTypeParam = "";
        if (!publishMode)
        {
            principalTypeParam = """
                ,
                    "principalType": ""
                """;
        }
        var expectedManifest = $$"""
            {
              "type": "azure.bicep.v0",
              "connectionString": "Server=tcp:{sql.outputs.sqlServerFqdn},1433;Encrypt=True;Authentication=\u0022Active Directory Default\u0022",
              "path": "sql.module.bicep",
              "params": {
                "principalId": "",
                "principalName": ""{{principalTypeParam}}
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var allowAllIpsFirewall = "";
        var bicepPrincipalTypeParam = "";
        var bicepPrincipalTypeSetter = "";
        if (!publishMode)
        {
            allowAllIpsFirewall = """

                resource sqlFirewallRule_AllowAllIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
                  name: 'AllowAllIps'
                  properties: {
                    endIpAddress: '255.255.255.255'
                    startIpAddress: '0.0.0.0'
                  }
                  parent: sql
                }
                
                """;

            bicepPrincipalTypeParam = """
                
                param principalType string
                
                """;

            bicepPrincipalTypeSetter = """

                      principalType: principalType
                """;
        }

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string
            {{bicepPrincipalTypeParam}}
            resource sql 'Microsoft.Sql/servers@2021-11-01' = {
              name: take('sql-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administrators: {
                  administratorType: 'ActiveDirectory'{{bicepPrincipalTypeSetter}}
                  login: principalName
                  sid: principalId
                  tenantId: subscription().tenantId
                  azureADOnlyAuthentication: true
                }
                minimalTlsVersion: '1.2'
                publicNetworkAccess: 'Enabled'
                version: '12.0'
              }
              tags: {
                'aspire-resource-name': 'sql'
              }
            }

            resource sqlFirewallRule_AllowAllAzureIps 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: sql
            }
            {{allowAllIpsFirewall}}
            resource db1 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db1'
              location: location
              properties: {
                freeLimitExhaustionBehavior: 'AutoPause'
                useFreeLimit: true
              }
              sku: {
                name: 'GP_S_Gen5_2'
              }
              parent: sql
            }

            resource db2 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db2Name'
              location: location
              properties: {
                freeLimitExhaustionBehavior: 'AutoPause'
                useFreeLimit: true
              }
              sku: {
                name: 'GP_S_Gen5_2'
              }
              parent: sql
            }

            resource db3 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db3Name'
              location: location
              sku: {
                name: 'HS_Gen5_2'
              }
              parent: sql
            }

            resource db4 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db4Name'
              location: location
              sku: {
                name: 'Basic'
              }
              parent: sql
            }

            resource db5 'Microsoft.Sql/servers/databases@2021-11-01' = {
              name: 'db5Name'
              location: location
              properties: {
                freeLimitExhaustionBehavior: 'AutoPause'
                useFreeLimit: true
              }
              sku: {
                name: 'GP_S_Gen5_2'
              }
              parent: sql
            }

            output sqlServerFqdn string = sql.properties.fullyQualifiedDomainName

            output name string = sql.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
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
        IResourceBuilder<AzureSqlDatabaseResource> db3 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db4 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db5 = null!;

        if (addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
            db3 = sql.AddDatabase("db3", "db3Name", "HS_Gen5_2");
            db4 = sql.AddDatabase("db4", "db4Name", "Basic");
            db5 = sql.AddDatabase("db5", "db5Name", "Free");
        }
        sql.RunAsContainer(c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
        });

        if (!addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
            db3 = sql.AddDatabase("db3", "db3Name", "HS_Gen5_2");
            db4 = sql.AddDatabase("db4", "db4Name", "Basic");
            db5 = sql.AddDatabase("db5", "db5Name", "Free");
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

        var db3ConnectionString = await db3.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db3ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db3Name", db3ConnectionString);

        var db4ConnectionString = await db4.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db4ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db4Name", db4ConnectionString);

        var db5ConnectionString = await db5.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=", db5ConnectionString);
        Assert.EndsWith(";TrustServerCertificate=true;Database=db5Name", db5ConnectionString);
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
        IResourceBuilder<AzureSqlDatabaseResource> db3 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db4 = null!;
        IResourceBuilder<AzureSqlDatabaseResource> db5 = null!;

        if (addDbBeforeRunAsContainer)
        {
            db1 = sql.AddDatabase("db1");
            db2 = sql.AddDatabase("db2", "db2Name");
            db3 = sql.AddDatabase("db3", "db3Name", "HS_Gen5_2");
            db4 = sql.AddDatabase("db4", "db4Name", "Basic");
            db5 = sql.AddDatabase("db5", "db5Name", "Free");
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
            db3 = sql.AddDatabase("db3", "db3Name", "HS_Gen5_2");
            db4 = sql.AddDatabase("db4", "db4Name", "Basic");
            db5 = sql.AddDatabase("db5", "db5Name", "Free");
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

        var db3ConnectionString = await db3.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=db3Name", db3ConnectionString);

        var db4ConnectionString = await db4.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=db4Name", db4ConnectionString);

        var db5ConnectionString = await db5.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Server=127.0.0.1,12455;User ID=sa;Password=p@ssw0rd1;TrustServerCertificate=true;Database=db5Name", db5ConnectionString);
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
