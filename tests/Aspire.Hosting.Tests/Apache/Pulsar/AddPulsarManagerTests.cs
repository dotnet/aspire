// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.RegularExpressions;
using Aspire.Hosting.Apache.Pulsar;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Apache.Pulsar;

public sealed class AddPulsarManagerTests
{
    [Fact]
    public void AddPulsarManagerContainerWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.AddPulsar("pulsar").WithPulsarManager("pulsar-manager");

        using var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<PulsarManagerResource>());
        Assert.Equal("pulsar-manager", containerResource.Name);

        var endpoints = containerResource.Annotations.OfType<EndpointAnnotation>().ToArray();
        Assert.Equal(2, endpoints.Length);

        var backendEndpoint = endpoints.Single(x => x.Name == "backend");
        Assert.Equal(7750, backendEndpoint.TargetPort);
        Assert.False(backendEndpoint.IsExternal);
        Assert.Null(backendEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, backendEndpoint.Protocol);
        Assert.Equal("http", backendEndpoint.Transport);
        Assert.Equal("http", backendEndpoint.UriScheme);

        var frontendEndpoint = endpoints.Single(x => x.Name == "frontend");
        Assert.Equal(9527, frontendEndpoint.TargetPort);
        Assert.False(frontendEndpoint.IsExternal);
        Assert.Null(frontendEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, frontendEndpoint.Protocol);
        Assert.Equal("http", frontendEndpoint.Transport);
        Assert.Equal("http", frontendEndpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(PulsarManagerContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(PulsarManagerContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(PulsarManagerContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public async Task PulsarManagerAddsStandardEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var envVars = await GetEnvironmentVariables(builder);

        Assert.Equal("/pulsar-manager/pulsar-manager/application.properties", envVars["SPRING_CONFIGURATION_FILE"]);
    }

    [Fact]
    public void PulsarManagerDuplicateInvocationDoesNotCreateNewContainerResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        builder.AddPulsar("pulsar")
            .WithPulsarManager()
            .WithPulsarManager();

        Assert.Single(builder.Resources.OfType<PulsarManagerResource>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithBookKeeperVisualManagerAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPulsar("pulsar").WithPulsarManager(null, null, c =>
            {
                if (isReadOnly.HasValue)
                {
                    c.WithBookKeeperVisualManager("mydata", isReadOnly: isReadOnly.Value);
                }
                else
                {
                    c.WithBookKeeperVisualManager("mydata");
                }
            }
        );

        var pulsarManager = Assert.Single(builder.Resources.OfType<PulsarManagerResource>());

        var volumeAnnotation = pulsarManager.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/pulsar-manager/pulsar-manager/bkvm.conf", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithApplicationPropertiesAddsMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.AddPulsar("pulsar").WithPulsarManager(null, null, c =>
            {
                if (isReadOnly.HasValue)
                {
                    c.WithApplicationProperties("mydata", isReadOnly: isReadOnly.Value);
                }
                else
                {
                    c.WithApplicationProperties("mydata");
                }
            }
        );

        var pulsarManager = Assert.Single(builder.Resources.OfType<PulsarManagerResource>());

        var volumeAnnotation = pulsarManager.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/pulsar-manager/pulsar-manager/application.properties", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public async Task PulsarManagerWithDefinedDefaultEnvironmentAddsDefaultEnvironmentEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var envVars = await GetEnvironmentVariables(builder,
            x => x.WithDefaultEnvironment("test-default-environment")
        );

        Assert.Equal("test-default-environment", envVars["DEFAULT_ENVIRONMENT_NAME"]);
        Assert.Equal("{pulsar.bindings.broker.url}", envVars["DEFAULT_ENVIRONMENT_BOOKIE_URL"]);
        Assert.Equal("{pulsar.bindings.service.url}", envVars["DEFAULT_ENVIRONMENT_SERVICE_URL"]);
    }

    [Fact]
    public async Task PulsarManagerWithTagSupportingDefinedDefaultSuperUserAddsEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var userNameParameter = builder.AddParameter("user");
        var emailParameter = builder.AddParameter("email");
        var passwordParameter = builder.AddParameter("password");

        var envVars = await GetEnvironmentVariables(builder,
            x => x
                // [>v0.4.0] supports default superuser via env
                .WithImageTag("v0.4.1")
                .WithDefaultSuperUser(
                    userNameParameter,
                    emailParameter,
                    passwordParameter
                )
        );

        Assert.Equal("true", envVars["DEFAULT_SUPERUSER_ENABLED"]);
        Assert.Equal("{user.value}", envVars["DEFAULT_SUPERUSER_NAME"]);
        Assert.Equal("{email.value}", envVars["DEFAULT_SUPERUSER_EMAIL"]);
        Assert.Equal("{password.value}", envVars["DEFAULT_SUPERUSER_PASSWORD"]);
    }

    [Theory]
    [InlineData(PulsarManagerContainerImageTags.Image, "v0.4.0")]
    [InlineData(PulsarManagerContainerImageTags.Image, "")]
    [InlineData(PulsarManagerContainerImageTags.Image, null)]
    [InlineData("someimage", PulsarManagerContainerImageTags.Tag)]
    public void PulsarManagerThrowsForUnsupportedImageForDefaultSuperUser(string? image, string? tag)
    {
        var builder = DistributedApplication.CreateBuilder();
        var pulsar = builder.AddPulsar("pulsar");

        Assert.Throws<DistributedApplicationException>(() => pulsar
            .WithPulsarManager(null, null,
                c => c
                    .WithImage(image!)
                    .WithImageTag(tag!)
                    .WithDefaultSuperUser()
            )
        );
    }

    private static async ValueTask<Dictionary<string, string>> GetEnvironmentVariables(
        IDistributedApplicationBuilder builder,
        Action<IResourceBuilder<PulsarManagerResource>>? containerConfiguration = null
    )
    {
        builder.AddPulsar("pulsar").WithPulsarManager(null, null, containerConfiguration);

        await using var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var pulsarManager = Assert.Single(appModel.Resources.OfType<PulsarManagerResource>());

        return await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(
            pulsarManager,
            DistributedApplicationOperation.Publish
        );
    }

    [Theory]
    [InlineData(6000)]
    [InlineData(null)]
    public async Task VerifyPulsarManagerManifest(int? port)
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();
        appBuilder
            .AddPulsar("pulsar")
            .WithPulsarManager(
                "pulsar-manager",
                port
            );

        var pulsarManager = appBuilder.Resources.OfType<PulsarManagerResource>().Single();

        var manifest = (await ManifestUtils.GetManifest(
            pulsarManager
        )).ToString();

        var expectedManifest = $$$"""
          {
            "type": "container.v0",
            "image": "{{{PulsarManagerContainerImageTags.Registry}}}/{{{PulsarManagerContainerImageTags.Image}}}:{{{PulsarManagerContainerImageTags.Tag}}}",
            "env": {
              "SPRING_CONFIGURATION_FILE": "/pulsar-manager/pulsar-manager/application.properties"
            },
            "bindings": {
              "frontend": {
                "scheme": "http",
                "protocol": "tcp",
                "transport": "http",
                {{{PortManifestPart(port)}}}
                "targetPort": 9527
              },
              "backend": {
                "scheme": "http",
                "protocol": "tcp",
                "transport": "http",
                "targetPort": 7750
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
