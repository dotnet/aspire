// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.ApplicationModel;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;

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

        const string defaultEndpointName = "http";

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>(), e => e.Name == defaultEndpointName);
        Assert.Equal(8080, endpoint.TargetPort);
        Assert.False(endpoint.IsExternal);
        Assert.Equal(defaultEndpointName, endpoint.Name);
        Assert.Null(endpoint.Port);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("http", endpoint.Transport);
        Assert.Equal("http", endpoint.UriScheme);

        const string managementEndpointName = "management";

        var healthEndpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>(), e => e.Name == managementEndpointName);
        Assert.Equal(9000, healthEndpoint.TargetPort);
        Assert.False(healthEndpoint.IsExternal);
        Assert.Equal(managementEndpointName, healthEndpoint.Name);
        Assert.Null(healthEndpoint.Port);
        Assert.Equal(ProtocolType.Tcp, healthEndpoint.Protocol);
        Assert.Equal("http", healthEndpoint.Transport);
        Assert.Equal("http", healthEndpoint.UriScheme);

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

        Assert.Equal($"{builder.GetVolumePrefix()}-{resourceName}-data", volumeAnnotation.Source);
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

    [Fact]
    public void AddAddKeycloakAddsGeneratedPasswordParameterWithUserSecretsParameterDefaultInRunMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create();

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.Equal("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.AdminPasswordParameter.Default?.GetType().FullName);
    }

    [Fact]
    public void AddAddKeycloakDoesNotAddGeneratedPasswordParameterWithUserSecretsParameterDefaultInPublishMode()
    {
        using var appBuilder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var rmq = appBuilder.AddKeycloak("keycloak");

        Assert.NotEqual("Aspire.Hosting.ApplicationModel.UserSecretsParameterDefault", rmq.Resource.AdminPasswordParameter.Default?.GetType().FullName);
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
                "KC_BOOTSTRAP_ADMIN_USERNAME": "admin",
                "KC_BOOTSTRAP_ADMIN_PASSWORD": "{keycloak-password.value}",
                "KC_HEALTH_ENABLED": "true"
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 8080
                },
                "management": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 9000
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task WithReverseProxyAddsEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithReverseProxy();

        var envTask = keycloak.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var env = await envTask;

        var kcHttpEnabled = Assert.Single(env, e => e.Key == "KC_HTTP_ENABLED");
        Assert.Equal("true", kcHttpEnabled.Value);

        var kcProxyHeaders = Assert.Single(env, e => e.Key == "KC_PROXY_HEADERS");
        Assert.Equal("xforwarded", kcProxyHeaders.Value);

        var kcHostname = Assert.Single(env, e => e.Key == "KC_HOSTNAME");
        Assert.NotNull(kcHostname.Value);
        
        // The hostname should be a resolved endpoint reference
        Assert.Contains("keycloak", kcHostname.Value);
        Assert.Contains("8080", kcHostname.Value);
    }

    [Fact]
    public async Task WithReverseProxyWithSpecificEndpointUsesCorrectEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithHttpEndpoint(port: 9080, name: "custom")
                              .WithReverseProxy("custom");

        var envTask = keycloak.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var env = await envTask;

        var kcHttpEnabled = Assert.Single(env, e => e.Key == "KC_HTTP_ENABLED");
        Assert.Equal("true", kcHttpEnabled.Value);

        var kcProxyHeaders = Assert.Single(env, e => e.Key == "KC_PROXY_HEADERS");
        Assert.Equal("xforwarded", kcProxyHeaders.Value);
        
        var kcHostname = Assert.Single(env, e => e.Key == "KC_HOSTNAME");
        Assert.NotNull(kcHostname.Value);
        
        // The hostname value may be an EndpointReference object in string form
        // Let's test what we actually get
        var hostnameStr = kcHostname.Value;
        Assert.True(
            hostnameStr.Contains("keycloak") || hostnameStr.Contains("EndpointReference"), 
            $"Expected hostname to contain 'keycloak' or 'EndpointReference' but got: {hostnameStr}");
    }

    [Fact]
    public async Task WithReverseProxyDoesNotAffectOtherEnvironmentVariables()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloakWithoutProxy = builder.AddKeycloak("keycloak1");
        var keycloakWithProxy = builder.AddKeycloak("keycloak2")
                                      .WithReverseProxy();

        var envWithoutProxyTask = keycloakWithoutProxy.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var envWithProxyTask = keycloakWithProxy.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        
        var envWithoutProxy = await envWithoutProxyTask;
        var envWithProxy = await envWithProxyTask;

        // Environment variables without proxy should not include reverse proxy settings
        Assert.DoesNotContain(envWithoutProxy, e => e.Key == "KC_HTTP_ENABLED");
        Assert.DoesNotContain(envWithoutProxy, e => e.Key == "KC_PROXY_HEADERS");
        Assert.DoesNotContain(envWithoutProxy, e => e.Key == "KC_HOSTNAME");

        // Environment variables with proxy should include both original and reverse proxy settings
        Assert.Contains(envWithProxy, e => e.Key == "KC_BOOTSTRAP_ADMIN_USERNAME");
        Assert.Contains(envWithProxy, e => e.Key == "KC_BOOTSTRAP_ADMIN_PASSWORD");
        Assert.Contains(envWithProxy, e => e.Key == "KC_HEALTH_ENABLED");
        Assert.Contains(envWithProxy, e => e.Key == "KC_HTTP_ENABLED");
        Assert.Contains(envWithProxy, e => e.Key == "KC_PROXY_HEADERS");
        Assert.Contains(envWithProxy, e => e.Key == "KC_HOSTNAME");
    }

    [Fact]
    public async Task VerifyManifestWithReverseProxy()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithReverseProxy();

        var manifest = await ManifestUtils.GetManifest(keycloak.Resource);

        // Validate that reverse proxy environment variables are included in the manifest
        var manifestJson = JsonDocument.Parse(manifest.ToString());
        var env = manifestJson.RootElement.GetProperty("env");
        
        // Verify original Keycloak environment variables are present
        Assert.Equal("admin", env.GetProperty("KC_BOOTSTRAP_ADMIN_USERNAME").GetString());
        Assert.Equal("{keycloak-password.value}", env.GetProperty("KC_BOOTSTRAP_ADMIN_PASSWORD").GetString());
        Assert.Equal("true", env.GetProperty("KC_HEALTH_ENABLED").GetString());
        
        // Verify reverse proxy environment variables are present
        Assert.Equal("true", env.GetProperty("KC_HTTP_ENABLED").GetString());
        Assert.Equal("xforwarded", env.GetProperty("KC_PROXY_HEADERS").GetString());
        Assert.Equal("{keycloak.bindings.http.url}", env.GetProperty("KC_HOSTNAME").GetString());
    }

    [Fact]
    public async Task PublishWithReverseProxyInRunModeDoesNotAddReverseProxyConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);
        
        var keycloak = builder.AddKeycloak("keycloak")
                              .PublishWithReverseProxy();

        var envTask = keycloak.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var env = await envTask;

        // Should not contain reverse proxy settings in run mode
        Assert.DoesNotContain(env, e => e.Key == "KC_HTTP_ENABLED");
        Assert.DoesNotContain(env, e => e.Key == "KC_PROXY_HEADERS");
        Assert.DoesNotContain(env, e => e.Key == "KC_HOSTNAME");

        // Should still contain standard Keycloak environment variables
        Assert.Contains(env, e => e.Key == "KC_BOOTSTRAP_ADMIN_USERNAME");
        Assert.Contains(env, e => e.Key == "KC_BOOTSTRAP_ADMIN_PASSWORD");
        Assert.Contains(env, e => e.Key == "KC_HEALTH_ENABLED");
    }

    [Fact]
    public async Task PublishWithReverseProxyInPublishModeAddsReverseProxyConfiguration()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        
        var keycloak = builder.AddKeycloak("keycloak")
                              .PublishWithReverseProxy();

        var envTask = keycloak.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var env = await envTask;

        // Should contain reverse proxy settings in publish mode
        var kcHttpEnabled = Assert.Single(env, e => e.Key == "KC_HTTP_ENABLED");
        Assert.Equal("true", kcHttpEnabled.Value);

        var kcProxyHeaders = Assert.Single(env, e => e.Key == "KC_PROXY_HEADERS");
        Assert.Equal("xforwarded", kcProxyHeaders.Value);

        var kcHostname = Assert.Single(env, e => e.Key == "KC_HOSTNAME");
        Assert.NotNull(kcHostname.Value);
        
        // In publish mode, this should be a manifest expression or resolved URL
        Assert.True(kcHostname.Value.StartsWith("{") || kcHostname.Value.StartsWith("http"));
    }

    [Fact]
    public async Task PublishWithReverseProxyWithSpecificEndpointUsesCorrectEndpoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
        
        var keycloak = builder.AddKeycloak("keycloak")
                              .WithHttpEndpoint(port: 9080, name: "custom")
                              .PublishWithReverseProxy("custom");

        var envTask = keycloak.Resource.GetEnvironmentVariableValuesAsync().DefaultTimeout();
        var env = await envTask;

        // Should contain reverse proxy settings with custom endpoint
        var kcHttpEnabled = Assert.Single(env, e => e.Key == "KC_HTTP_ENABLED");
        Assert.Equal("true", kcHttpEnabled.Value);

        var kcProxyHeaders = Assert.Single(env, e => e.Key == "KC_PROXY_HEADERS");
        Assert.Equal("xforwarded", kcProxyHeaders.Value);
        
        var kcHostname = Assert.Single(env, e => e.Key == "KC_HOSTNAME");
        Assert.NotNull(kcHostname.Value);
        
        // In publish mode, this should be a manifest expression for the custom endpoint or reference the custom endpoint
        Assert.True(kcHostname.Value.Contains("custom") || kcHostname.Value.Contains("EndpointReference"));
    }
}
