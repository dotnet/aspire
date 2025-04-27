// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzurePostgresExtensionsTests(ITestOutputHelper output)
{
    [Theory]
    // [InlineData(true, true)] this scenario is covered in RoleAssignmentTests.PostgresSupport. The output doesn't match the pattern here because the role assignment isn't generated
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task AddAzurePostgresFlexibleServer(bool publishMode, bool useAcaInfrastructure)
    {
        using var builder = TestDistributedApplicationBuilder.Create(publishMode ? DistributedApplicationOperation.Publish : DistributedApplicationOperation.Run);

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");

        if (useAcaInfrastructure)
        {
            builder.AddAzureContainerAppEnvironment("env");

            // on ACA infrastructure, if there are no references to the postgres resource,
            // then there won't be any roles created. So add a reference here.
            builder.AddContainer("api", "myimage")
                .WithReference(postgres);
        }

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, TestContext.Current.CancellationToken);

        var manifest = await AzureManifestUtils.GetManifestWithBicep(postgres.Resource, skipPreparer: true);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{postgres-data.outputs.connectionString}",
              "path": "postgres-data.module.bicep"
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var allowAllIpsFirewall = "";
        if (!publishMode)
        {
            allowAllIpsFirewall = """

                resource postgreSqlFirewallRule_AllowAllIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
                  name: 'AllowAllIps'
                  properties: {
                    endIpAddress: '255.255.255.255'
                    startIpAddress: '0.0.0.0'
                  }
                  parent: postgres_data
                }
            
                """;
        }

        var expectedBicep = $$"""
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource postgres_data 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
              name: take('postgresdata-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                authConfig: {
                  activeDirectoryAuth: 'Enabled'
                  passwordAuth: 'Disabled'
                }
                availabilityZone: '1'
                backup: {
                  backupRetentionDays: 7
                  geoRedundantBackup: 'Disabled'
                }
                highAvailability: {
                  mode: 'Disabled'
                }
                storage: {
                  storageSizeGB: 32
                }
                version: '16'
              }
              sku: {
                name: 'Standard_B1ms'
                tier: 'Burstable'
              }
              tags: {
                'aspire-resource-name': 'postgres-data'
              }
            }

            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres_data
            }
            {{allowAllIpsFirewall}}
            output connectionString string = 'Host=${postgres_data.properties.fullyQualifiedDomainName}'

            output name string = postgres_data.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var postgresRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>().Where(r => r.Name == $"postgres-data-roles"));
        var postgresRolesManifest = await AzureManifestUtils.GetManifestWithBicep(postgresRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param postgres_data_outputs_name string

            param principalType string

            param principalId string

            param principalName string

            resource postgres_data 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' existing = {
              name: postgres_data_outputs_name
            }

            resource postgres_data_admin 'Microsoft.DBforPostgreSQL/flexibleServers/administrators@2024-08-01' = {
              name: principalId
              properties: {
                principalName: principalName
                principalType: principalType
              }
              parent: postgres_data
            }
            """;
        output.WriteLine(postgresRolesManifest.BicepText);
        Assert.Equal(expectedBicep, postgresRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddAzurePostgresFlexibleServer_WithPasswordAuthentication_NoKeyVaultWithContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddAzurePostgresFlexibleServer("pg").WithPasswordAuthentication().RunAsContainer();

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, CancellationToken.None);

        Assert.Empty(model.Resources.OfType<AzureKeyVaultResource>());
    }

    [Theory]
    [InlineData(true, true, null)]
    [InlineData(true, true, "mykeyvault")]
    [InlineData(true, false, null)]
    [InlineData(true, false, "mykeyvault")]
    [InlineData(false, true, null)]
    [InlineData(false, true, "mykeyvault")]
    [InlineData(false, false, null)]
    [InlineData(false, false, "mykeyvault")]
    public async Task AddAzurePostgresWithPasswordAuth(bool specifyUserName, bool specifyPassword, string? kvName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var userName = specifyUserName ? builder.AddParameter("user") : null;
        var password = specifyPassword ? builder.AddParameter("password") : null;

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");

        if (kvName is null)
        {
            kvName = "postgres-data-kv";
            postgres.WithPasswordAuthentication(userName, password);
        }
        else
        {
            var keyVault = builder.AddAzureKeyVault(kvName);
            postgres.WithPasswordAuthentication(keyVault, userName, password);
        }

        postgres.AddDatabase("db1", "db1Name");

        var manifest = await AzureManifestUtils.GetManifestWithBicep(postgres.Resource);

        var expectedManifest = $$"""
            {
              "type": "azure.bicep.v0",
              "connectionString": "{{{kvName}}.secrets.connectionstrings--postgres-data}",
              "path": "postgres-data.module.bicep",
              "params": {
                "administratorLogin": "{{{userName?.Resource.Name ?? "postgres-data-username"}}.value}",
                "administratorLoginPassword": "{{{password?.Resource.Name ?? "postgres-data-password"}}.value}",
                "keyVaultName": "{{{kvName}}.outputs.name}"
              }
            }
            """;

        var m = manifest.ManifestNode.ToString();
        output.WriteLine(m);
        Assert.Equal(expectedManifest, m);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location
            
            param administratorLogin string
            
            @secure()
            param administratorLoginPassword string
            
            param keyVaultName string
            
            resource postgres_data 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
              name: take('postgresdata-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                administratorLogin: administratorLogin
                administratorLoginPassword: administratorLoginPassword
                authConfig: {
                  activeDirectoryAuth: 'Disabled'
                  passwordAuth: 'Enabled'
                }
                availabilityZone: '1'
                backup: {
                  backupRetentionDays: 7
                  geoRedundantBackup: 'Disabled'
                }
                highAvailability: {
                  mode: 'Disabled'
                }
                storage: {
                  storageSizeGB: 32
                }
                version: '16'
              }
              sku: {
                name: 'Standard_B1ms'
                tier: 'Burstable'
              }
              tags: {
                'aspire-resource-name': 'postgres-data'
              }
            }
            
            resource postgreSqlFirewallRule_AllowAllAzureIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllAzureIps'
              properties: {
                endIpAddress: '0.0.0.0'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres_data
            }
            
            resource postgreSqlFirewallRule_AllowAllIps 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
              name: 'AllowAllIps'
              properties: {
                endIpAddress: '255.255.255.255'
                startIpAddress: '0.0.0.0'
              }
              parent: postgres_data
            }
            
            resource db1 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
              name: 'db1Name'
              parent: postgres_data
            }
            
            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }
            
            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--postgres-data'
              properties: {
                value: 'Host=${postgres_data.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword}'
              }
              parent: keyVault
            }
            
            resource db1_connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--db1'
              properties: {
                value: 'Host=${postgres_data.properties.fullyQualifiedDomainName};Username=${administratorLogin};Password=${administratorLoginPassword};Database=db1Name'
              }
              parent: keyVault
            }
            
            output name string = postgres_data.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzurePostgresFlexibleServerRunAsContainerProducesCorrectConnectionString(bool addDbBeforeRunAsContainer)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");

        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> db1 = null!;
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> db2 = null!;
        if (addDbBeforeRunAsContainer)
        {
            db1 = postgres.AddDatabase("db1");
            db2 = postgres.AddDatabase("db2", "db2Name");

        }
        postgres.RunAsContainer(c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
        });

        if (!addDbBeforeRunAsContainer)
        {
            db1 = postgres.AddDatabase("db1");
            db2 = postgres.AddDatabase("db2", "db2Name");
        }

        Assert.True(postgres.Resource.IsContainer(), "The resource should now be a container resource.");
        Assert.StartsWith("Host=localhost;Port=12455;Username=postgres;Password=", await postgres.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));

        var db1ConnectionString = await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Host=localhost;Port=12455;Username=postgres;Password=", db1ConnectionString);
        Assert.EndsWith("Database=db1", db1ConnectionString);

        var db2ConnectionString = await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.StartsWith("Host=localhost;Port=12455;Username=postgres;Password=", db2ConnectionString);
        Assert.EndsWith("Database=db2Name", db2ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAzurePostgresFlexibleServerRunAsContainerProducesCorrectUserNameAndPasswordAndHost(bool addDbBeforeRunAsContainer)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");
        var pass = builder.AddParameter("pass", "p@ssw0rd1");
        var user = builder.AddParameter("user", "user1");

        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> db1 = null!;
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> db2 = null!;
        if (addDbBeforeRunAsContainer)
        {
            db1 = postgres.AddDatabase("db1");
            db2 = postgres.AddDatabase("db2", "db2Name");
        }

        IResourceBuilder<PostgresServerResource>? innerPostgres = null;
        postgres.RunAsContainer(configureContainer: c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
                .WithHostPort(12455)
                .WithPassword(pass)
                .WithUserName(user);
            innerPostgres = c;
        });

        if (!addDbBeforeRunAsContainer)
        {
            db1 = postgres.AddDatabase("db1");
            db2 = postgres.AddDatabase("db2", "db2Name");
        }

        Assert.NotNull(innerPostgres);

        var endpoint = Assert.Single(innerPostgres.Resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(5432, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(12455, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        Assert.True(postgres.Resource.IsContainer(), "The resource should now be a container resource.");
        Assert.Equal("Host=localhost;Port=12455;Username=user1;Password=p@ssw0rd1", await postgres.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));

        var db1ConnectionString = await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.Equal("Host=localhost;Port=12455;Username=user1;Password=p@ssw0rd1;Database=db1", db1ConnectionString);

        var db2ConnectionString = await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
        Assert.Equal("Host=localhost;Port=12455;Username=user1;Password=p@ssw0rd1;Database=db2Name", db2ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WithPasswordAuthenticationBeforeAfterRunAsContainer(bool before)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var usr = builder.AddParameter("usr", "user");
        var pwd = builder.AddParameter("pwd", "p@ssw0rd1", secret: true);

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");

        if (before)
        {
            postgres.WithPasswordAuthentication(usr, pwd);
        }

        postgres.RunAsContainer(c =>
        {
            c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
        });

        if (!before)
        {
            postgres.WithPasswordAuthentication(usr, pwd);
        }

        var db1 = postgres.AddDatabase("db1");
        var db2 = postgres.AddDatabase("db2", "db2Name");

        Assert.Equal("Host=localhost;Port=12455;Username=user;Password=p@ssw0rd1", await postgres.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("Host=localhost;Port=12455;Username=user;Password=p@ssw0rd1;Database=db1", await db1.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
        Assert.Equal("Host=localhost;Port=12455;Username=user;Password=p@ssw0rd1;Database=db2Name", await db2.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void RunAsContainerAppliesAnnotationsCorrectly(bool annotationsBefore, bool addDatabaseBefore)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var postgres = builder.AddAzurePostgresFlexibleServer("postgres-data");
        IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource>? db = null;

        if (addDatabaseBefore)
        {
            db = postgres.AddDatabase("db1");
        }

        if (annotationsBefore)
        {
            postgres.WithAnnotation(new Dummy1Annotation());
            db?.WithAnnotation(new Dummy1Annotation());
        }

        postgres.RunAsContainer(c =>
        {
            c.WithAnnotation(new Dummy2Annotation());
        });

        if (!addDatabaseBefore)
        {
            db = postgres.AddDatabase("db1");

            if (annotationsBefore)
            {
                // need to add the annotation here in this case becuase it has to be added after the DB is created
                db!.WithAnnotation(new Dummy1Annotation());
            }
        }

        if (!annotationsBefore)
        {
            postgres.WithAnnotation(new Dummy1Annotation());
            db!.WithAnnotation(new Dummy1Annotation());
        }

        var postgresResourceInModel = builder.Resources.Single(r => r.Name == "postgres-data");
        var dbResourceInModel = builder.Resources.Single(r => r.Name == "db1");

        Assert.True(postgresResourceInModel.TryGetAnnotationsOfType<Dummy1Annotation>(out var postgresAnnotations1));
        Assert.Single(postgresAnnotations1);

        Assert.True(postgresResourceInModel.TryGetAnnotationsOfType<Dummy2Annotation>(out var postgresAnnotations2));
        Assert.Single(postgresAnnotations2);

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
