// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Garnet;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Garnet;

public class AddGarnetTests
{
    [Fact]
    public void AddGarnetContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddGarnet("myGarnet").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<GarnetResource>());
        Assert.Equal("myGarnet", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(GarnetContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(GarnetContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(GarnetContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddGarnetContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddGarnet("myGarnet", port: 8813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<GarnetResource>());
        Assert.Equal("myGarnet", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(8813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(GarnetContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(GarnetContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(GarnetContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task GarnetCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddGarnet("myGarnet")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        await using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.Equal("{myGarnet.bindings.tcp.host}:{myGarnet.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet");

        var manifest = await ManifestUtils.GetManifest(garnet.Resource);

        var expectedManifest = $$"""
                                 {
                                   "type": "container.v0",
                                   "connectionString": "{myGarnet.bindings.tcp.host}:{myGarnet.bindings.tcp.port}",
                                   "image": "{{GarnetContainerImageTags.Registry}}/{{GarnetContainerImageTags.Image}}:{{GarnetContainerImageTags.Tag}}",
                                   "bindings": {
                                     "tcp": {
                                       "scheme": "tcp",
                                       "protocol": "tcp",
                                       "transport": "tcp",
                                       "targetPort": 6379
                                     }
                                   }
                                 }
                                 """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public void WithRedisCommanderAddsRedisCommanderResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet1").WithRedisCommander();
        builder.AddGarnet("mygarnet2").WithRedisCommander();

        Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingContainerImageValues()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet").WithRedisCommander(c => {
            c.WithImageRegistry("example.mycompany.com");
            c.WithImage("customrediscommander");
            c.WithImageTag("someothertag");
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var containerAnnotation = Assert.Single(resource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal("example.mycompany.com", containerAnnotation.Registry);
        Assert.Equal("customrediscommander", containerAnnotation.Image);
        Assert.Equal("someothertag", containerAnnotation.Tag);
    }

    [Fact]
    public void WithRedisCommanderSupportsChangingHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddGarnet("mygarnet").WithRedisCommander(c => {
            c.WithHostPort(1000);
        });

        var resource = Assert.Single(builder.Resources.OfType<RedisCommanderResource>());
        var endpoint = Assert.Single(resource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(1000, endpoint.Port);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task SingleGarnetInstanceProducesCorrectGarnetHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var garnet = builder.AddGarnet("mygarnet1").WithRedisCommander();
        await using var app = builder.Build();

        // Add fake allocated endpoints.
        garnet.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal($"mygarnet1:{containerHost}:5001:0", config["GARNET_HOSTS"]);
    }

    [Theory]
    [InlineData("host.docker.internal")]
    [InlineData("host.containers.internal")]
    public async Task MultipleGarnetInstanceProducesCorrectGarnetHostsVariable(string containerHost)
    {
        var builder = DistributedApplication.CreateBuilder();
        var garnet1 = builder.AddGarnet("mygarnet1").WithRedisCommander();
        var garnet2 = builder.AddGarnet("mygarnet2").WithRedisCommander();
        await using var app = builder.Build();

        // Add fake allocated endpoints.
        garnet1.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5001, containerHost));
        garnet2.WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 5002, "host2"));

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();
        var hook = new RedisCommanderConfigWriterHook();
        await hook.AfterEndpointsAllocatedAsync(model, CancellationToken.None);

        var commander = builder.Resources.Single(r => r.Name.EndsWith("-commander"));

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(commander);

        Assert.Equal($"mygarnet1:{containerHost}:5001:0,mygarnet2:host2:5002:0", config["GARNET_HOSTS"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet");
        if (isReadOnly.HasValue)
        {
            garnet.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            garnet.WithDataVolume();
        }

        var volumeAnnotation = garnet.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal("Aspire.Hosting.Tests-myGarnet-data", volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet");
        if (isReadOnly.HasValue)
        {
            garnet.WithDataBindMount("mygarnetdata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            garnet.WithDataBindMount("mygarnetdata");
        }

        var volumeAnnotation = garnet.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mygarnetdata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                              .WithDataVolume();

        Assert.True(garnet.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 60 1".Split(" "), args);
    }

    [Fact]
    public void WithDataVolumeDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                           .WithDataVolume(isReadOnly: true);

        var persistenceAnnotation = garnet.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                           .WithDataBindMount("mygarnetdata");

        Assert.True(garnet.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 60 1".Split(" "), args);
    }

    [Fact]
    public void WithDataBindMountDoesNotAddPersistenceAnnotationIfIsReadOnly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                           .WithDataBindMount("mygarnetdata", isReadOnly: true);

        var persistenceAnnotation = garnet.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        Assert.True(garnet.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

        var args = new List<object>();
        foreach (var argsAnnotation in argsCallbacks)
        {
            Assert.NotNull(argsAnnotation.Callback);
            argsAnnotation.Callback(new CommandLineArgsCallbackContext(args));
        }

        Assert.Equal("--save 10 2".Split(" "), args);
    }

    [Fact]
    public void WithPersistenceAddsCommandLineArgsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var garnet = builder.AddGarnet("myGarnet")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.True(garnet.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.NotNull(argsAnnotations.SingleOrDefault());
    }
}
