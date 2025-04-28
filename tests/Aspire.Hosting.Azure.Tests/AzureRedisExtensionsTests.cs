// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureRedisExtensionsTests(ITestOutputHelper output)
{
    /// <summary>
    /// Test both with and without ACA infrastructure because the role assignments
    /// are handled differently between the two. This ensures that the bicep is generated
    /// consistently regardless of the infrastructure used in RunMode.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAzureRedis(bool useAcaInfrastructure)
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        if (useAcaInfrastructure)
        {
            builder.AddAzureContainerAppEnvironment("env");
        }

        var redis = builder.AddAzureRedis("redis-cache");

        builder.AddContainer("api", "myimage")
            .WithReference(redis);

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var manifest = await GetManifestWithBicep(model, redis.Resource);

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            resource redis_cache 'Microsoft.Cache/redis@2024-03-01' = {
              name: take('rediscache-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'Basic'
                  family: 'C'
                  capacity: 1
                }
                enableNonSslPort: false
                disableAccessKeyAuthentication: true
                minimumTlsVersion: '1.2'
                redisConfiguration: {
                  'aad-enabled': 'true'
                }
              }
              tags: {
                'aspire-resource-name': 'redis-cache'
              }
            }

            output connectionString string = '${redis_cache.properties.hostName},ssl=true'

            output name string = redis_cache.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);

        var redisRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "redis-cache-roles");
        var redisRolesManifest = await AzureManifestUtils.GetManifestWithBicep(redisRoles, skipPreparer: true);
        expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param redis_cache_outputs_name string

            param principalId string

            param principalName string

            resource redis_cache 'Microsoft.Cache/redis@2024-03-01' existing = {
              name: redis_cache_outputs_name
            }

            resource redis_cache_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
              name: guid(redis_cache.id, principalId, 'Data Contributor')
              properties: {
                accessPolicyName: 'Data Contributor'
                objectId: principalId
                objectIdAlias: principalName
              }
              parent: redis_cache
            }
            """;
        output.WriteLine(redisRolesManifest.BicepText);
        Assert.Equal(expectedBicep, redisRolesManifest.BicepText);
    }

    [Fact]
    public async Task AddAzureRedis_WithAccessKeyAuthentication_NoKeyVaultWithContainer()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        builder.AddAzureRedis("redis").WithAccessKeyAuthentication().RunAsContainer();

        var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        await ExecuteBeforeStartHooksAsync(app, CancellationToken.None);

        Assert.Empty(model.Resources.OfType<AzureKeyVaultResource>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("mykeyvault")]
    public async Task AddAzureRedisWithAccessKeyAuthentication(string? kvName)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddAzureRedis("redis-cache");

        if (kvName is null)
        {
            kvName = "redis-cache-kv";

            redis.WithAccessKeyAuthentication();
        }
        else
        {
            redis.WithAccessKeyAuthentication(builder.AddAzureKeyVault(kvName));
        }

        var manifest = await AzureManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = $$"""
            {
              "type": "azure.bicep.v0",
              "connectionString": "{{{kvName}}.secrets.connectionstrings--redis-cache}",
              "path": "redis-cache.module.bicep",
              "params": {
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
            
            param keyVaultName string
            
            resource redis_cache 'Microsoft.Cache/redis@2024-03-01' = {
              name: take('rediscache-${uniqueString(resourceGroup().id)}', 63)
              location: location
              properties: {
                sku: {
                  name: 'Basic'
                  family: 'C'
                  capacity: 1
                }
                enableNonSslPort: false
                minimumTlsVersion: '1.2'
              }
              tags: {
                'aspire-resource-name': 'redis-cache'
              }
            }
            
            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }
            
            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionstrings--redis-cache'
              properties: {
                value: '${redis_cache.properties.hostName},ssl=true,password=${redis_cache.listKeys().primaryKey}'
              }
              parent: keyVault
            }
            
            output name string = redis_cache.name
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureRedisRunAsContainerProducesCorrectConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        RedisResource? redisResource = null;
        var redis = builder.AddAzureRedis("cache")
            .RunAsContainer(c =>
            {
                redisResource = c.Resource;

                c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
            });

        Assert.True(redis.Resource.IsContainer(), "The resource should now be a container resource.");

        Assert.NotNull(redisResource?.PasswordParameter);
        Assert.Equal($"localhost:12455,password={redisResource.PasswordParameter.Value}", await redis.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AddAzureRedisRunAsContainerProducesCorrectHostAndPassword()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pass = builder.AddParameter("pass", "p@ssw0rd1");

        RedisResource? redisResource = null;
        var redis = builder.AddAzureRedis("cache")
            .RunAsContainer(c =>
            {
                redisResource = c.Resource;

                c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455))
                    .WithHostPort(12455)
                    .WithPassword(pass);
            });

        Assert.NotNull(redisResource);

        var endpoint = Assert.Single(redisResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(12455, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        Assert.True(redis.Resource.IsContainer(), "The resource should now be a container resource.");

        Assert.NotNull(redisResource?.PasswordParameter);
        Assert.Equal($"localhost:12455,password=p@ssw0rd1", await redis.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RunAsContainerAppliesAnnotationsCorrectly(bool before)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var cache = builder.AddAzureRedis("cache");

        if (before)
        {
            cache.WithAnnotation(new Dummy1Annotation());
        }

        cache.RunAsContainer(c =>
        {
            c.WithAnnotation(new Dummy2Annotation());
        });

        if (!before)
        {
            cache.WithAnnotation(new Dummy1Annotation());
        }

        var cacheInModel = builder.Resources.Single(r => r.Name == "cache");

        Assert.True(cacheInModel.TryGetAnnotationsOfType<Dummy1Annotation>(out var cacheAnnotations1));
        Assert.Single(cacheAnnotations1);

        Assert.True(cacheInModel.TryGetAnnotationsOfType<Dummy2Annotation>(out var cacheAnnotations2));
        Assert.Single(cacheAnnotations2);
    }

    private sealed class Dummy1Annotation : IResourceAnnotation
    {
    }

    private sealed class Dummy2Annotation : IResourceAnnotation
    {
    }
}
