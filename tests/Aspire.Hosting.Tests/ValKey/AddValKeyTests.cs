// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.Utils;
using Aspire.Hosting.ValKey;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.ValKey;

public class AddValKeyTests
{
    [Fact]
    public void AddValKeyContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValKey("myValKey").PublishAsContainer();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ValKeyResource>());
        Assert.Equal("myValKey", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ValKeyContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ValKeyContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ValKeyContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddValKeyContainerAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValKey("myValKey", port: 8813);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<ValKeyResource>());
        Assert.Equal("myValKey", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(6379, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("tcp", endpoint.Name);
        Assert.Equal(8813, endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("tcp", endpoint.Transport);
        Assert.Equal("tcp", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(ValKeyContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(ValKeyContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(ValKeyContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task ValKeyCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddValKey("myValKey")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        await using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<IResourceWithConnectionString>());
        var connectionString = await connectionStringResource.GetConnectionStringAsync(default);
        Assert.Equal("{myValKey.bindings.tcp.host}:{myValKey.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
        Assert.StartsWith("localhost:2000", connectionString);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valKey = builder.AddValKey("myValKey");

        var manifest = await ManifestUtils.GetManifest(valKey.Resource);

        var expectedManifest = $$"""
                                 {
                                   "type": "container.v0",
                                   "connectionString": "{myValKey.bindings.tcp.host}:{myValKey.bindings.tcp.port}",
                                   "image": "{{ValKeyContainerImageTags.Registry}}/{{ValKeyContainerImageTags.Image}}:{{ValKeyContainerImageTags.Tag}}",
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

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valKey = builder.AddValKey("myValKey");
        if (isReadOnly.HasValue)
        {
            valKey.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            valKey.WithDataVolume();
        }

        var volumeAnnotation = valKey.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal("Aspire.Hosting.Tests-myValKey-data", volumeAnnotation.Source);
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
        var valKey = builder.AddValKey("myValKeydata");
        if (isReadOnly.HasValue)
        {
            valKey.WithDataBindMount("myValKeydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            valKey.WithDataBindMount("myValKeydata");
        }

        var volumeAnnotation = valKey.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "myValKeydata"), volumeAnnotation.Source);
        Assert.Equal("/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valKey = builder.AddValKey("myValKey")
                              .WithDataVolume();

        Assert.True(valKey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

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
        var valKey = builder.AddValKey("myValKey")
                           .WithDataVolume(isReadOnly: true);

        var persistenceAnnotation = valKey.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithDataBindMountAddsPersistenceAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valKey = builder.AddValKey("myValKey")
                           .WithDataBindMount("myvalKeydata");

        Assert.True(valKey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

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
        var valKey = builder.AddValKey("myValKey")
                           .WithDataBindMount("myvalKeydata", isReadOnly: true);

        var persistenceAnnotation = valKey.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().SingleOrDefault();

        Assert.Null(persistenceAnnotation);
    }

    [Fact]
    public void WithPersistenceReplacesPreviousAnnotationInstances()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var valKey = builder.AddValKey("myValKey")
                           .WithDataVolume()
                           .WithPersistence(TimeSpan.FromSeconds(10), 2);

        Assert.True(valKey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsCallbacks));

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
        var valKey = builder.AddValKey("myValKey")
                           .WithPersistence(TimeSpan.FromSeconds(60));

        Assert.True(valKey.Resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var argsAnnotations));
        Assert.NotNull(argsAnnotations.SingleOrDefault());
    }
}
