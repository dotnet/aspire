// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aspire.Hosting.Apache.Pulsar;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Apache.Pulsar;

public class AddPulsarTests
{
    [Fact]
    public void AddPulsarContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPulsar("pulsar");

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<PulsarResource>());
        Assert.Equal("pulsar", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>().ToArray();
        Assert.Equal(2, endpoints.Length);

        var brokerEndpoint = endpoints.Single(x => x.Name == "broker");
        Assert.Equal(6650, brokerEndpoint.TargetPort);
        Assert.False(brokerEndpoint.IsExternal);
        Assert.Null(brokerEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, brokerEndpoint.Protocol);
        Assert.Equal("tcp", brokerEndpoint.Transport);
        Assert.Equal("pulsar", brokerEndpoint.UriScheme);

        var serviceEndpoint = endpoints.Single(x => x.Name == "service");
        Assert.Equal(8080, serviceEndpoint.TargetPort);
        Assert.False(serviceEndpoint.IsExternal);
        Assert.Null(serviceEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, serviceEndpoint.Protocol);
        Assert.Equal("http", serviceEndpoint.Transport);
        Assert.Equal("http", serviceEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(PulsarContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(PulsarContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(PulsarContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task PulsarCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddPulsar("pulsar")
            .WithEndpoint("broker", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017));

        await using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<PulsarResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("pulsar://localhost:27017", connectionString);
        Assert.Equal("{pulsar.bindings.broker.url}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public void PulsarAddsSingleStandaloneCommandLineArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pulsar = builder.AddPulsar("pulsar")
            .AsStandalone()
            .AsStandalone();

        Assert.True(pulsar.Resource.TryGetAnnotationsOfType<StandalonePulsarCommandLineArgsAnnotation>(out var argsAnnotations));
        Assert.NotNull(argsAnnotations.Single());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pulsar = builder.AddPulsar("pulsar");
        if (isReadOnly.HasValue)
        {
            pulsar.WithDataVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            pulsar.WithDataVolume();
        }

        var volumeAnnotation = pulsar.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var appName = builder.Environment.ApplicationName;
        Assert.Equal($"{appName}-pulsar-pulsardata", volumeAnnotation.Source);
        Assert.Equal("/pulsar/data", volumeAnnotation.Target);
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
        var pulsar = builder.AddPulsar("pulsar");
        if (isReadOnly.HasValue)
        {
            pulsar.WithDataBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            pulsar.WithDataBindMount("mydata");
        }

        var volumeAnnotation = pulsar.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/pulsar/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithConfigVolumeAddsVolumeAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pulsar = builder.AddPulsar("pulsar");
        if (isReadOnly.HasValue)
        {
            pulsar.WithConfigVolume(isReadOnly: isReadOnly.Value);
        }
        else
        {
            pulsar.WithConfigVolume();
        }

        var volumeAnnotation = pulsar.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        var appName = builder.Environment.ApplicationName;
        Assert.Equal($"{appName}-pulsar-pulsarconf", volumeAnnotation.Source);
        Assert.Equal("/pulsar/conf", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithConfigBindMountAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var pulsar = builder.AddPulsar("pulsar");
        if (isReadOnly.HasValue)
        {
            pulsar.WithConfigBindMount("mydata", isReadOnly: isReadOnly.Value);
        }
        else
        {
            pulsar.WithConfigBindMount("mydata");
        }

        var volumeAnnotation = pulsar.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/pulsar/conf", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(6000, null)]
    [InlineData(null, 6000)]
    [InlineData(6000, 7000)]
    public async Task VerifyPulsarManifest(int? brokerPort, int? servicePort)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var manifest = (await ManifestUtils.GetManifest(
            appBuilder.AddPulsar(
                name: "pulsar",
                targetPort: servicePort,
                brokerPort: brokerPort
            ).Resource
        )).ToString();

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{pulsar.bindings.broker.url}",
              "image": "{{PulsarContainerImageTags.Registry}}/{{PulsarContainerImageTags.Image}}:{{PulsarContainerImageTags.Tag}}",
              "entrypoint": "/bin/bash",
              "args": [
                "-c",
                "bin/apply-config-from-env.py conf/standalone.conf \u0026\u0026 bin/pulsar standalone"
              ],
              "bindings": {
                "service": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  {{PortManifestPart(servicePort)}}
                  "targetPort": 8080
                },
                "broker": {
                  "scheme": "pulsar",
                  "protocol": "tcp",
                  "transport": "tcp",
                  {{PortManifestPart(brokerPort)}}
                  "targetPort": 6650
                }
              }
            }
            """;

        // removes blank lines
        expectedManifest = Regex.Replace(
            expectedManifest,
            @"^\s*$\n",
            string.Empty,
            RegexOptions.Multiline
        );

        Assert.Equal(expectedManifest, manifest);
    }

    private static string PortManifestPart(int? port) => port is null ? string.Empty : $"\"port\": {port},";
}
