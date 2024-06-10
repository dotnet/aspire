// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace Aspire.Hosting.Containers.Tests;

public class WithDockerfileTests
{
    [Fact]
    public async Task WithDockerfileLaunchesContainerSuccessfully()
    {
        if (!IsDockerAvailable())
        {
            return;
        }

        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath);

        using var app = builder.Build();
        await app.StartAsync();

        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html"); // Proves the container built, ran, and contains customizations!

        Assert.Equal($"{DefaultMessage}\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.Single<Container>(containers);
        Assert.Equal(tempContextPath, container!.Spec!.Build!.Context);
        Assert.Equal(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);

        await app.StopAsync();
    }

    [Fact]
    public async Task WithDockerfileResultsInBuildAttributeBeingAddedToManifest()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });

        var parameter = builder.AddParameter("message");
        builder.Configuration["Parameters:message"] = "hello";

        var container = builder.AddContainer("testcontainer", "testimage")
                               .WithHttpEndpoint(targetPort: 80)
                               .WithDockerfile(tempContextPath, tempDockerfilePath, "runner")
                               .WithBuildArg("MESSAGE", parameter)
                               .WithBuildArg("stringParam", "a string")
                               .WithBuildArg("intParam", 42);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "stage": "runner",
                "args": {
                  "MESSAGE": "{message.value}",
                  "stringParam": "a string",
                  "intParam": "42"
                }
              },
              "bindings": {
                "http": {
                  "scheme": "http",
                  "protocol": "tcp",
                  "transport": "http",
                  "targetPort": 80
                }
              }
            }
            """;
        Assert.Equal(expectedManifest, manifest.ToString());
    }

    [Fact]
    public async Task WithDockerfileWithParameterLaunchesContainerSuccessfully()
    {
        if (!IsDockerAvailable())
        {
            return;
        }

        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var parameter = builder.AddParameter("message");
        builder.Configuration["Parameters:message"] = "hello";

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath)
               .WithBuildArg("MESSAGE", parameter)
               .WithBuildArg("stringParam", "a string")
               .WithBuildArg("intParam", 42)
               .WithBuildArg("boolParamTrue", true)
               .WithBuildArg("boolParamFalse", false);

        using var app = builder.Build();
        await app.StartAsync();

        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html"); // Proves the container built, ran, and contains customizations!

        Assert.Equal($"hello\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.Single<Container>(containers);
        Assert.Equal(tempContextPath, container!.Spec!.Build!.Context);
        Assert.Equal(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);
        Assert.Null(container!.Spec!.Build!.Stage);
        Assert.Collection(
            container!.Spec!.Build!.Args!,
            arg =>
            {
                Assert.Equal("MESSAGE", arg.Name);
                Assert.Equal("hello", arg.Value);
            },
            arg =>
            {
                Assert.Equal("stringParam", arg.Name);
                Assert.Equal("a string", arg.Value);
            },
            arg =>
            {
                Assert.Equal("intParam", arg.Name);
                Assert.Equal("42", arg.Value);
            },
            arg =>
            {
                Assert.Equal("boolParamTrue", arg.Name);
                Assert.Equal("true", arg.Value);
            },
            arg =>
            {
                Assert.Equal("boolParamFalse", arg.Name);
                Assert.Equal("false", arg.Value);
            }
            );

        await app.StopAsync();
    }

    [Fact]
    public void WithDockerfileWithEmptyContextPathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile(string.Empty);
        });

        Assert.Equal("contextPath", ex.ParamName);
    }

    [Fact]
    public void WithDockerfileWithContextPathThatDoesNotExistThrowsDirectoryNotFoundException()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<DirectoryNotFoundException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile("a/path/to/nowhere");
        });
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathAndEmptyDockerfilePathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, _) = await CreateTemporaryDockerfileAsync(createDockerfile: false);

        var ex = Assert.Throws<FileNotFoundException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile(tempContextPath, string.Empty);
        });
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathAndInvalidDockerfilePathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, _) = await CreateTemporaryDockerfileAsync();

        var ex = Assert.Throws<FileNotFoundException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile(tempContextPath, "Notarealdockerfile");
        });
    }

    [Fact]
    public void WithBuildArgsBeforeFromDockerfileThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var container = builder.AddContainer("mycontainer", "myimage");

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            container.WithBuildArg("MESSAGE", "hello");
        });

        Assert.Equal(
            "The resource does not have a Dockerfile build annotation. Call WithDockerfile before calling WithBuildArg.",
            ex.Message
            );
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithImplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Dockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitNonDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync("Otherdockerfile");

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Otherdockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitAbsoluteDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, tempDockerfilePath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    private static async Task<(string ContextPath, string DockerfilePath)> CreateTemporaryDockerfileAsync(string dockerfileName = "Dockerfile", bool createDockerfile = true)
    {
        var tempContextPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempContextPath);

        var tempDockerfilePath = Path.Combine(tempContextPath, dockerfileName);

        if (createDockerfile)
        {
            await File.WriteAllTextAsync(tempDockerfilePath, HelloWorldDockerfile);
        }

        return (tempContextPath, tempDockerfilePath);
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo("docker", "info")
            {
                RedirectStandardError = true,
                RedirectStandardInput   = true,
                RedirectStandardOutput  = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);

            var completed = process!.WaitForExit(10000);

            if (!completed)
            {
                process.Kill();
            }

            if (!completed || process.ExitCode != 0)
            {
                throw new XunitException("Docker is available but not responding.");
            }
            else
            {
                return true;
            }

        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private const string DefaultMessage = "aspire!";

    private const string HelloWorldDockerfile = $$"""
        FROM mcr.microsoft.com/k8se/quickstart:latest AS builder
        ARG MESSAGE=aspire!
        RUN echo ${MESSAGE} > /app/static/aspire.html

        FROM mcr.microsoft.com/k8se/quickstart:latest AS runner
        ARG MESSAGE
        COPY --from=builder /app/static/aspire.html /app/static
        """;
}

