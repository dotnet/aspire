// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Certbot.Tests;

public class AddCertbotTests
{
    [Fact]
    public void AddCertbotContainerWithHttp01ChallengeAddsAnnotationMetadata()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        appBuilder.AddCertbot("certbot", domain, email)
            .WithHttp01Challenge();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<CertbotResource>());
        Assert.Equal("certbot", containerResource.Name);

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(80, endpoint.TargetPort);
        Assert.Equal(80, endpoint.Port);
        Assert.True(endpoint.IsExternal);
        Assert.Equal("http", endpoint.Name);
        Assert.Equal(ProtocolType.Tcp, endpoint.Protocol);
        Assert.Equal("http", endpoint.Transport);
        Assert.Equal("http", endpoint.UriScheme);

        var containerAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerImageAnnotation>());
        Assert.Equal(CertbotContainerImageTags.Tag, containerAnnotation.Tag);
        Assert.Equal(CertbotContainerImageTags.Image, containerAnnotation.Image);
        Assert.Equal(CertbotContainerImageTags.Registry, containerAnnotation.Registry);
    }

    [Fact]
    public void AddCertbotContainerHasVolumeAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        appBuilder.AddCertbot("certbot", domain, email);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<CertbotResource>());

        var volumeAnnotation = Assert.Single(containerResource.Annotations.OfType<ContainerMountAnnotation>());
        Assert.Equal("letsencrypt", volumeAnnotation.Source);
        Assert.Equal("/etc/letsencrypt", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void WithHttp01ChallengeAddsArgsAnnotation()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        appBuilder.AddCertbot("certbot", domain, email)
            .WithHttp01Challenge();

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<CertbotResource>());

        var argsAnnotations = containerResource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().ToList();
        Assert.NotEmpty(argsAnnotations);
    }

    [Fact]
    public void AddCertbotContainerStoresDomainAndEmailParameters()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        appBuilder.AddCertbot("certbot", domain, email);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<CertbotResource>());

        Assert.NotNull(containerResource.DomainParameter);
        Assert.Equal("domain", containerResource.DomainParameter.Name);
        Assert.NotNull(containerResource.EmailParameter);
        Assert.Equal("email", containerResource.EmailParameter.Name);
    }

    [Fact]
    public async Task VerifyManifestWithHttp01Challenge()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email)
            .WithHttp01Challenge();

        var manifest = await ManifestUtils.GetManifest(certbot.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "{{CertbotContainerImageTags.Registry}}/{{CertbotContainerImageTags.Image}}:{{CertbotContainerImageTags.Tag}}",
              "args": [
                "certonly",
                "--standalone",
                "--non-interactive",
                "--agree-tos",
                "-v",
                "--keep-until-expiring",
                "--email",
                "{email.value}",
                "-d",
                "{domain.value}"
              ],
              "volumes": [
                {
                  "name": "letsencrypt",
                  "target": "/etc/letsencrypt",
                  "readOnly": false
                }
              ],
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80,
                  "external": true
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task VerifyManifestWithHttp01ChallengeAndPermissionFix()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email)
            .WithHttp01Challenge()
            .WithPermissionFix();

        var manifest = await ManifestUtils.GetManifest(certbot.Resource);

        var expectedManifest = $$"""
            {
              "type": "container.v0",
              "image": "{{CertbotContainerImageTags.Registry}}/{{CertbotContainerImageTags.Image}}:{{CertbotContainerImageTags.Tag}}",
              "args": [
                "certonly",
                "--standalone",
                "--non-interactive",
                "--agree-tos",
                "-v",
                "--keep-until-expiring",
                "--email",
                "{email.value}",
                "-d",
                "{domain.value}",
                "--deploy-hook",
                "chmod -R 755 /etc/letsencrypt/live \u0026\u0026 chmod -R 755 /etc/letsencrypt/archive"
              ],
              "volumes": [
                {
                  "name": "letsencrypt",
                  "target": "/etc/letsencrypt",
                  "readOnly": false
                }
              ],
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80,
                  "external": true
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public void WithCertificateVolumeAddsVolumeAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email);

        var container = builder.AddContainer("test", "testimage")
                               .WithCertificateVolume(certbot);

        var volumes = container.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Single(volumes);

        var volumeAnnotation = volumes[0];
        Assert.Equal("letsencrypt", volumeAnnotation.Source);
        Assert.Equal("/etc/letsencrypt", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void WithCertificateVolumeWithCustomMountPath()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email);

        var container = builder.AddContainer("test", "testimage")
                               .WithCertificateVolume(certbot, "/custom/certs");

        var volumes = container.Resource.Annotations.OfType<ContainerMountAnnotation>().ToList();
        Assert.Single(volumes);

        var volumeAnnotation = volumes[0];
        Assert.Equal("letsencrypt", volumeAnnotation.Source);
        Assert.Equal("/custom/certs", volumeAnnotation.Target);
        Assert.Equal(ContainerMountType.Volume, volumeAnnotation.Type);
    }

    [Fact]
    public void AddCertbotThrowsWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        using var appBuilder = TestDistributedApplicationBuilder.Create();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        Assert.Throws<ArgumentNullException>(() => builder.AddCertbot("certbot", domain, email));
    }

    [Fact]
    public void AddCertbotThrowsWhenNameIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");

        Assert.Throws<ArgumentNullException>(() => builder.AddCertbot(null!, domain, email));
    }

    [Fact]
    public void AddCertbotThrowsWhenDomainIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var email = builder.AddParameter("email");

        Assert.Throws<ArgumentNullException>(() => builder.AddCertbot("certbot", null!, email));
    }

    [Fact]
    public void AddCertbotThrowsWhenEmailIsNull()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");

        Assert.Throws<ArgumentNullException>(() => builder.AddCertbot("certbot", domain, null!));
    }

    [Fact]
    public void CertificatePathExpressionContainsDomainParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email);

        var certificatePath = certbot.Resource.CertificatePath;

        Assert.NotNull(certificatePath);
        Assert.Equal("/etc/letsencrypt/live/{domain.value}/fullchain.pem", certificatePath.ValueExpression);
    }

    [Fact]
    public void PrivateKeyPathExpressionContainsDomainParameter()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var domain = builder.AddParameter("domain");
        var email = builder.AddParameter("email");
        var certbot = builder.AddCertbot("certbot", domain, email);

        var privateKeyPath = certbot.Resource.PrivateKeyPath;

        Assert.NotNull(privateKeyPath);
        Assert.Equal("/etc/letsencrypt/live/{domain.value}/privkey.pem", privateKeyPath.ValueExpression);
    }

    [Fact]
    public void WithHttp01ChallengeWithCustomPort()
    {
        var appBuilder = DistributedApplication.CreateBuilder();
        var domain = appBuilder.AddParameter("domain");
        var email = appBuilder.AddParameter("email");

        appBuilder.AddCertbot("certbot", domain, email)
            .WithHttp01Challenge(port: 8080);

        using var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResource = Assert.Single(appModel.Resources.OfType<CertbotResource>());

        var endpoint = Assert.Single(containerResource.Annotations.OfType<EndpointAnnotation>());
        Assert.Equal(80, endpoint.TargetPort);
        Assert.Equal(8080, endpoint.Port);
    }
}
