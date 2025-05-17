// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
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
        var (manifest, bicep) = await GetManifestWithBicep(model, redis.Resource);
        var redisRoles = Assert.Single(model.Resources.OfType<AzureProvisioningResource>(), r => r.Name == "redis-cache-roles");
        var (redisRolesManifest, redisRolesBicep) = await AzureManifestUtils.GetManifestWithBicep(redisRoles, skipPreparer: true);

        await Verify(manifest.ToString(), "json")
              .AppendContentAsFile(bicep, "bicep")
              .AppendContentAsFile(redisRolesManifest.ToString(), "json")
              .AppendContentAsFile(redisRolesBicep, "bicep");
              
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

        await Verify(manifest.BicepText, extension: "bicep");
            
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
