// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Keycloak.Tests;

public class KeycloakResourceBuilderTests
{
    [Fact]
    public void AddKeycloakWithDefaultsAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();

        var resourceName = "keycloak";
        appBuilder.AddKeycloak(resourceName);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<KeycloakResource>());
        Assert.Equal(resourceName, containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(8080, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal("http", endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("http", endpoint.Transport);
        Assert.Equal("http", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(KeycloakContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(KeycloakContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(KeycloakContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName)
                              .WithDataVolume();

        var volumeAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal($"Aspire.Hosting.Tests-{resourceName}-data", volumeAnnotation.Source);
        Assert.Equal("/opt/keycloak/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Fact]
    public void WithDataBindMountAddsMountAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithDataBindMount("mydata");

        var volumeAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(Path.Combine(builder.AppHostDirectory, "mydata"), volumeAnnotation.Source);
        Assert.Equal("/opt/keycloak/data", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, volumeAnnotation.Type);
        Assert.False(volumeAnnotation.IsReadOnly);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    [InlineData(false)]
    public void WithRealmImportAddsBindMountAnnotation(bool? isReadOnly)
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);

        var resourceName = "keycloak";
        var keycloak = builder.AddKeycloak(resourceName);

        if (isReadOnly.HasValue)
        {
            keycloak.WithRealmImport(tempDirectory, isReadOnly: isReadOnly.Value);
        }
        else
        {
            keycloak.WithRealmImport(tempDirectory);
        }

        var containerAnnotation = keycloak.Resource.Annotations.OfType<ContainerMountAnnotation>().Single();

        Assert.Equal(tempDirectory, containerAnnotation.Source);
        Assert.Equal("/opt/keycloak/data/import", containerAnnotation.Target);
        Assert.Equal(ContainerMountType.BindMount, containerAnnotation.Type);
        Assert.Equal(isReadOnly ?? false, containerAnnotation.IsReadOnly);
    }

    [Fact]
    public void AddAddKeycloakAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.IsType<UserSecretsParameterDefault>(rmq.Resource.AdminPasswordParameter.Default);
    }

    [Fact]
    public void AddAddKeycloakDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.IsNotType<UserSecretsParameterDefault>(rmq.Resource.AdminPasswordParameter.Default);
    }

    [Fact]
    public async Task VerifyManifest()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak");

        var manifest = await ManifestUtils.GetManifest(keycloak.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "{{KeycloakContainerImageTags.Registry}}/{{KeycloakContainerImageTags.Image}}:{{KeycloakContainerImageTags.Tag}}",
              "args": [
                "start-dev",
                "--import-realm"
              ],
              "env": {
                "KEYCLOAK_ADMIN": "admin",
                "KEYCLOAK_ADMIN_PASSWORD": "{keycloak-password.value}"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8080
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }
}
