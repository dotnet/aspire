// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Azure.Tests;

public class AzureRedisExtensionsTests(ITestOutputHelper output)
{
    [Fact]
    public async Task AddAzureRedis()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddAzureRedis("redis-cache");

        var manifest = await ManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{redis-cache.outputs.connectionString}",
              "path": "redis-cache.module.bicep",
              "params": {
                "principalId": "",
                "principalName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

        var expectedBicep = """
            @description('The location for the resource(s) to be deployed.')
            param location string = resourceGroup().location

            param principalId string

            param principalName string

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
                redisConfiguration: {
                  'aad-enabled': 'true'
                }
                disableAccessKeyAuthentication: 'true'
              }
              tags: {
                'aspire-resource-name': 'redis-cache'
              }
            }

            resource redis_cache_contributor 'Microsoft.Cache/redis/accessPolicyAssignments@2024-03-01' = {
              name: take('rediscachecontributor${uniqueString(resourceGroup().id)}', 24)
              properties: {
                accessPolicyName: 'Data Contributor'
                objectId: principalId
                objectIdAlias: principalName
              }
              parent: redis_cache
            }

            output connectionString string = '${redis_cache.properties.hostName},ssl=true'
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureRedisWithAccessKeyAuthentication()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddAzureRedis("redis-cache")
            .WithAccessKeyAuthentication();

        var manifest = await ManifestUtils.GetManifestWithBicep(redis.Resource);

        var expectedManifest = """
            {
              "type": "azure.bicep.v0",
              "connectionString": "{redis-cache.secretOutputs.connectionString}",
              "path": "redis-cache.module.bicep",
              "params": {
                "keyVaultName": ""
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ManifestNode.ToString());

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
                redisConfiguration: { }
              }
              tags: {
                'aspire-resource-name': 'redis-cache'
              }
            }

            resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
              name: keyVaultName
            }
            
            resource connectionString 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
              name: 'connectionString'
              properties: {
                value: '${redis_cache.properties.hostName},ssl=true,password=${redis_cache.listKeys().primaryKey}'
              }
              parent: keyVault
            }
            """;
        output.WriteLine(manifest.BicepText);
        Assert.Equal(expectedBicep, manifest.BicepText);
    }

    [Fact]
    public async Task AddAzureRedisRunAsContainerProducesCorrectConnectionString()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var redis = builder.AddAzureRedis("cache")
            .RunAsContainer(c =>
            {
                c.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 12455));
            });

        Assert.False(redis.Resource.IsContainer(), "The resource is still the Azure Resource.");

        Assert.Equal("localhost:12455", await redis.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None));
    }
}
