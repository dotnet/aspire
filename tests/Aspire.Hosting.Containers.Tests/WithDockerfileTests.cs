// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Dcp.Model;
using Aspire.Hosting.Testing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aspire.Hosting.Containers.Tests;

public class WithDockerfileTests(ITestOutputHelper testOutputHelper)
{
    [Fact]
    [RequiresDocker]
    public async Task WithBuildSecretPopulatesSecretFilesCorrectly()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync(includeSecrets: true);

        var parameter = builder.AddParameter("secret", secret: true);
        builder.Configuration["Parameters:secret"] = "open sesame from env";

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath)
               .WithBuildSecret("ENV_SECRET", parameter);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var envSecretMessage = await client.GetStringAsync("/ENV_SECRET.txt");
        Assert.Equal("open sesame from env", envSecretMessage);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task ContainerBuildLogsAreStreamedToAppHost()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(logging =>
        {
            logging.AddFakeLogging();
            logging.AddXunit(testOutputHelper);
        });

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath);

        using var app = builder.Build();

        await app.StartAsync();

        // Wait for the resource to come online.
        await WaitForResourceAsync(app, "testcontainer", "Running");
        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html");

        // By the time we can make a request to the service the logs
        // should be streamed back to the app host.
        var collector = app.Services.GetFakeLogCollector();
        var logs = collector.GetSnapshot();

        // Just looking for a common message in Docker build output.
        Assert.Contains(logs, log => log.Message.Contains("load build definition from Dockerfile"));

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task WithDockerfileLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddContainer("testcontainer", "testimage")
               .WithHttpEndpoint(targetPort: 80)
               .WithDockerfile(tempContextPath, tempDockerfilePath);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

        Assert.Equal($"{DefaultMessage}\n", message);

        var kubernetes = app.Services.GetRequiredService<IKubernetesService>();
        var containers = await kubernetes.ListAsync<Container>();

        var container = Assert.Single<Container>(containers);
        Assert.Equal(tempContextPath, container!.Spec!.Build!.Context);
        Assert.Equal(tempDockerfilePath, container!.Spec!.Build!.Dockerfile);

        await app.StopAsync();
    }

    [Fact]
    [RequiresDocker]
    public async Task AddDockerfileLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
               .WithHttpEndpoint(targetPort: 80);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");
        var message = await client.GetStringAsync("/aspire.html");

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
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

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
    public async Task AddDockerfileResultsInBuildAttributeBeingAddedToManifest()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var parameter = builder.AddParameter("message");
        builder.Configuration["Parameters:message"] = "hello";

        var container = builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath, "runner")
                               .WithHttpEndpoint(targetPort: 80)
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
    public async Task WithDockerfileWithBuildSecretResultsInManifestReferencingSecretParameter()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var parameter = builder.AddParameter("secret", secret: true);
        builder.Configuration["Parameters:secret"] = "open sesame";

        var container = builder.AddContainer("testcontainer", "testimage")
                               .WithHttpEndpoint(targetPort: 80)
                               .WithDockerfile(tempContextPath, tempDockerfilePath)
                               .WithBuildSecret("SECRET", parameter);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "secrets": {
                  "SECRET": {
                    "type": "env",
                    "value": "{secret.value}"
                  }
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
    public async Task AddDockerfileWithBuildSecretResultsInManifestReferencingSecretParameter()
    {
        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();
        var manifestOutputPath = Path.Combine(tempContextPath, "aspire-manifest.json");
        var builder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            Args = ["--publisher", "manifest", "--output-path", manifestOutputPath],
        });
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var parameter = builder.AddParameter("secret", secret: true);
        builder.Configuration["Parameters:secret"] = "open sesame";

        var container = builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
                               .WithHttpEndpoint(targetPort: 80)
                               .WithBuildSecret("SECRET", parameter);

        var manifest = await ManifestUtils.GetManifest(container.Resource, manifestDirectory: tempContextPath);
        var expectedManifest = $$$$"""
            {
              "type": "container.v1",
              "build": {
                "context": ".",
                "dockerfile": "Dockerfile",
                "secrets": {
                  "SECRET": {
                    "type": "env",
                    "value": "{secret.value}"
                  }
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
    [RequiresDocker]
    public async Task WithDockerfileWithParameterLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

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

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

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
    [RequiresDocker]
    public async Task AddDockerfileWithParameterLaunchesContainerSuccessfully()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var parameter = builder.AddParameter("message");
        builder.Configuration["Parameters:message"] = "hello";

        builder.AddDockerfile("testcontainer", tempContextPath, tempDockerfilePath)
               .WithHttpEndpoint(targetPort: 80)
               .WithBuildArg("MESSAGE", parameter)
               .WithBuildArg("stringParam", "a string")
               .WithBuildArg("intParam", 42)
               .WithBuildArg("boolParamTrue", true)
               .WithBuildArg("boolParamFalse", false);

        using var app = builder.Build();
        await app.StartAsync();

        await WaitForResourceAsync(app, "testcontainer", "Running");

        using var client = app.CreateHttpClient("testcontainer", "http");

        var message = await client.GetStringAsync("/aspire.html");

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
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            builder.AddContainer("mycontainer", "myimage")
                   .WithDockerfile(string.Empty);
        });

        Assert.Equal("contextPath", ex.ParamName);
    }

    [Fact]
    public void AddDockerfileWithEmptyContextPathThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            builder.AddDockerfile("mycontainer", string.Empty)
                   .WithDockerfile(string.Empty);
        });

        Assert.Equal("contextPath", ex.ParamName);
    }

    [Fact]
    public void WithBuildArgsBeforeWithDockerfileThrows()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

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
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithImplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Dockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath, "Dockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitNonDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync("Otherdockerfile");

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, "Otherdockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitNonDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync("Otherdockerfile");

        var container = builder.AddDockerfile("mycontainer", tempContextPath, "Otherdockerfile");

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task WithDockerfileWithValidContextPathValidDockerfileWithExplicitAbsoluteDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddContainer("mycontainer", "myimage")
                               .WithDockerfile(tempContextPath, tempDockerfilePath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    [Fact]
    public async Task AddDockerfileWithValidContextPathValidDockerfileWithExplicitAbsoluteDefaultNameSucceeds()
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        builder.Services.AddLogging(b => b.AddXunit(testOutputHelper));

        var (tempContextPath, tempDockerfilePath) = await CreateTemporaryDockerfileAsync();

        var container = builder.AddDockerfile("mycontainer", tempContextPath, tempDockerfilePath);

        var annotation = Assert.Single(container.Resource.Annotations.OfType<DockerfileBuildAnnotation>());
        Assert.Equal(tempContextPath, annotation.ContextPath);
        Assert.Equal(tempDockerfilePath, annotation.DockerfilePath);
    }

    private static async Task<(string ContextPath, string DockerfilePath)> CreateTemporaryDockerfileAsync(string dockerfileName = "Dockerfile", bool createDockerfile = true, bool includeSecrets = false)
    {
        var tempContextPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempContextPath);

        var tempDockerfilePath = Path.Combine(tempContextPath, dockerfileName);

        if (createDockerfile)
        {
            var dockerfileTemplate = includeSecrets ? HelloWorldDockerfileWithSecrets : HelloWorldDockerfile;
            // We apply this random value to the Dockerfile to make sure that we get a clean
            // build each time with no possible caching.
            var cacheBuster = Guid.NewGuid();
            var dockerfileContent = dockerfileTemplate.Replace("!!!CACHEBUSTER!!!", cacheBuster.ToString());

            await File.WriteAllTextAsync(tempDockerfilePath, dockerfileContent);
        }

        return (tempContextPath, tempDockerfilePath);
    }

    private static async Task WaitForResourceAsync(DistributedApplication app, string resourceName, string resourceState, TimeSpan? timeout = null)
    {
        var rns = app.Services.GetRequiredService<ResourceNotificationService>();
        await rns.WaitForResourceAsync(resourceName, resourceState).WaitAsync(timeout ?? TimeSpan.FromMinutes(3));
    }

    private const string DefaultMessage = "aspire!";

    private const string HelloWorldDockerfile = $$"""
        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS builder
        ARG MESSAGE=aspire!
        RUN mkdir -p /usr/share/nginx/html
        RUN echo !!!CACHEBUSTER!!! > /usr/share/nginx/html/cachebuster.txt
        RUN echo ${MESSAGE} > /usr/share/nginx/html/aspire.html

        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS runner
        ARG MESSAGE
        RUN mkdir -p /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/cachebuster.txt /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/aspire.html /usr/share/nginx/html
        """;

    private const string HelloWorldDockerfileWithSecrets = $$"""
        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS builder
        ARG MESSAGE=aspire!
        RUN mkdir -p /usr/share/nginx/html
        RUN echo !!!CACHEBUSTER!!! > /usr/share/nginx/html/cachebuster.txt
        RUN echo ${MESSAGE} > /usr/share/nginx/html/aspire.html

        FROM mcr.microsoft.com/cbl-mariner/base/nginx:1.22 AS runner
        ARG MESSAGE
        RUN mkdir -p /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/cachebuster.txt /usr/share/nginx/html
        COPY --from=builder /usr/share/nginx/html/aspire.html /usr/share/nginx/html
        RUN --mount=type=secret,id=ENV_SECRET cp /run/secrets/ENV_SECRET /usr/share/nginx/html/ENV_SECRET.txt
        RUN chmod -R 777 /usr/share/nginx/html
        """;
}
