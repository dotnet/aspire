// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Kafka.Tests;

public class AddKafkaTests
{
    [Fact]
    public void AddKafkaContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddKafka("kafka");

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<KafkaServerResource>());
        Assert.Equal("kafka", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>();
        Assert.Equal(2, endpoints.Count());

        var primaryEndpoint = Assert.Single(endpoints, e => e.Name == "tcp");
        Assert.Equal(9092, primaryEndpoint.TargetPort);
        Assert.False(primaryEndpoint.IsExternal);
        Assert.Equal("tcp", primaryEndpoint.Name);
        Assert.Null(primaryEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, primaryEndpoint.Protocol);
        Assert.Equal("tcp", primaryEndpoint.Transport);
        Assert.Equal("tcp", primaryEndpoint.UriScheme);

        var internalEndpoint = Assert.Single(endpoints, e => e.Name == "internal");
        Assert.Equal(9093, internalEndpoint.TargetPort);
        Assert.False(internalEndpoint.IsExternal);
        Assert.Equal("internal", internalEndpoint.Name);
        Assert.Null(internalEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, internalEndpoint.Protocol);
        Assert.Equal("tcp", internalEndpoint.Transport);
        Assert.Equal("tcp", internalEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(KafkaContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(KafkaContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(KafkaContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task KafkaCreatesConnectionString()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder
            .AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017));

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var connectionStringResource = Assert.Single(appModel.Resources.OfType<KafkaServerResource>()) as IResourceWithConnectionString;
        var connectionString = await connectionStringResource.GetConnectionStringAsync();

        Assert.Equal("localhost:27017", connectionString);
        Assert.Equal("{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}", connectionStringResource.ConnectionStringExpression.ValueExpression);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka");

        var manifest = await ManifestUtils.GetManifest(kafka.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "connectionString": "{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port}",
              "image": "{{KafkaContainerImageTags.Registry}}/{{KafkaContainerImageTags.Image}}:{{KafkaContainerImageTags.Tag}}",
              "env": {
                "KAFKA_LISTENERS": "PLAINTEXT://localhost:29092,CONTROLLER://localhost:29093,PLAINTEXT_HOST://0.0.0.0:9092,PLAINTEXT_INTERNAL://0.0.0.0:9093",
                "KAFKA_LISTENER_SECURITY_PROTOCOL_MAP": "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT",
                "KAFKA_ADVERTISED_LISTENERS": "PLAINTEXT://{kafka.bindings.tcp.host}:29092,PLAINTEXT_HOST://{kafka.bindings.tcp.host}:{kafka.bindings.tcp.port},PLAINTEXT_INTERNAL://{kafka.bindings.internal.host}:{kafka.bindings.internal.port}"
              },
              "bindings": {
                "tcp": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9092
                },
                "internal": {
                  "scheme": "tcp",
                  "protocol": "tcp",
                  "transport": "tcp",
                  "targetPort": 9093
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task WithDataVolumeConfigureCorrectEnvironment()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .WithDataVolume("kafka-data");

        var config = await kafka.Resource.GetEnvironmentVariableValuesAsync();

        var volumeAnnotation = kafka.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal("kafka-data", volumeAnnotation.Source);
        Assert.Equal("/var/lib/kafka/data", volumeAnnotation.Target);
        Assert.Contains(config, kvp => kvp.Key == "KAFKA_LOG_DIRS" && kvp.Value == "/var/lib/kafka/data");
    }

    [Fact]
    public async Task WithDataBindConfigureCorrectEnvironment()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var kafka = appBuilder.AddKafka("kafka")
            .WithEndpoint("tcp", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 27017))
            .WithDataBindMount("kafka-data");

        var config = await kafka.Resource.GetEnvironmentVariableValuesAsync();

        var volumeAnnotation = kafka.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(appBuilder.AppHostDirectory, "kafka-data"), volumeAnnotation.Source);
        Assert.Equal("/var/lib/kafka/data", volumeAnnotation.Target);
        Assert.Contains(config, kvp => kvp.Key == "KAFKA_LOG_DIRS" && kvp.Value == "/var/lib/kafka/data");
    }

    public static TheoryData<string?, string, int?> WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallbackTestVariations()
    {
        return new()
        {
            { "kafka-ui", "kafka-ui", 8081 },
            { null, "kafka1-kafka-ui", 8081 },
            { "kafka-ui", "kafka-ui", null },
            { null, "kafka1-kafka-ui", null },
        };
    }

    [Theory]
    [MemberData(nameof(WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallbackTestVariations))]
    public void WithKafkaUIAddsAnUniqueContainerSetsItsNameAndInvokesConfigurationCallback(string? containerName, string expectedContainerName, int? port)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var configureContainerInvocations = 0;
        Action<IResourceBuilder<KafkaUIContainerResource>> kafkaUIConfigurationCallback = kafkaUi =>
        {
            kafkaUi.WithHostPort(port);
            configureContainerInvocations++;
        };
        builder.AddKafka("kafka1").WithKafkaUI(configureContainer: kafkaUIConfigurationCallback, containerName: containerName);
        builder.AddKafka("kafka2").WithKafkaUI();

        Assert.Single(builder.Resources.OfType<KafkaUIContainerResource>());
        var kafkaUiResource = Assert.Single(builder.Resources, r => r.Name == expectedContainerName);
        Assert.Equal(1, configureContainerInvocations);
        var kafkaUiEndpoint = kafkaUiResource.Annotations.OfType<EndpointAnnotation>().Single();
        Assert.Equal(8080, kafkaUiEndpoint.TargetPort);
        Assert.Equal(port, kafkaUiEndpoint.Port);
    }
}
