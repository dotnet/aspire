// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.ApplicationModel.Docker;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests.ApplicationModel.Docker;

public class WithDockerfileBuilderTests
{
    [Fact]
    public void WithDockerfile_WithCallback_CreatesCallbackAnnotation()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        // Act
        container.WithDockerfile("context", context =>
        {
            context.Builder.From("alpine", "latest");
        });

        // Assert
        var callbackAnnotation = container.Resource.Annotations.OfType<DockerfileBuildCallbackAnnotation>().LastOrDefault();
        Assert.NotNull(callbackAnnotation);
        Assert.Single(callbackAnnotation.Callbacks);
    }

    [Fact]
    public void WithDockerfile_MultipleCallbacks_AppendsToAnnotation()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        // Act
        container.WithDockerfile("context", context =>
        {
            context.Builder.From("alpine", "latest");
        });

        container.WithDockerfile("context", context =>
        {
            context.Builder.Stages[0].WorkDir("/app");
        });

        // Assert
        var callbackAnnotation = container.Resource.Annotations.OfType<DockerfileBuildCallbackAnnotation>().LastOrDefault();
        Assert.NotNull(callbackAnnotation);
        Assert.Equal(2, callbackAnnotation.Callbacks.Count);
    }

    [Fact]
    public void WithDockerfile_CreatesDockerfileBuildAnnotation()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        // Act
        container.WithDockerfile("context", context =>
        {
            context.Builder.From("alpine", "latest");
        });

        // Assert
        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);
        Assert.NotNull(buildAnnotation.DockerfileFactory);
    }

    [Fact]
    public async Task WithDockerfile_GeneratesDockerfileFromCallback()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        container.WithDockerfile("context", context =>
        {
            context.Builder.From("alpine", "latest")
                .WorkDir("/app")
                .Run("apk add curl")
                .Copy(".", ".")
                .Cmd(["./myapp"]);
        });

        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);
        Assert.NotNull(buildAnnotation.DockerfileFactory);

        var factoryContext = new DockerfileFactoryContext
        {
            Services = appBuilder.Services.BuildServiceProvider(),
            Resource = container.Resource,
            CancellationToken = CancellationToken.None
        };

        // Act
        var dockerfile = await buildAnnotation.DockerfileFactory(factoryContext);

        // Assert
        Assert.Contains("FROM alpine:latest", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("RUN apk add curl", dockerfile);
        Assert.Contains("COPY . .", dockerfile);
        Assert.Contains("CMD [\"./myapp\"]", dockerfile);
    }

    [Fact]
    public async Task WithDockerfile_MultipleCallbacks_BuildsComposedDockerfile()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        // First callback - base setup
        container.WithDockerfile("context", context =>
        {
            context.Builder.From("node", "18")
                .WorkDir("/app");
        });

        // Second callback - add dependencies
        container.WithDockerfile("context", context =>
        {
            context.Builder.Stages[0]
                .Copy("package*.json", "./")
                .Run("npm ci");
        });

        // Third callback - add application
        container.WithDockerfile("context", context =>
        {
            context.Builder.Stages[0]
                .Copy(".", ".")
                .Cmd(["node", "index.js"]);
        });

        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);

        var factoryContext = new DockerfileFactoryContext
        {
            Services = appBuilder.Services.BuildServiceProvider(),
            Resource = container.Resource,
            CancellationToken = CancellationToken.None
        };

        // Act
        var dockerfile = await buildAnnotation.DockerfileFactory!(factoryContext);

        // Assert
        Assert.Contains("FROM node:18", dockerfile);
        Assert.Contains("WORKDIR /app", dockerfile);
        Assert.Contains("COPY package*.json ./", dockerfile);
        Assert.Contains("RUN npm ci", dockerfile);
        Assert.Contains("COPY . .", dockerfile);
        Assert.Contains("CMD [\"node\",\"index.js\"]", dockerfile);
    }

    [Fact]
    public async Task WithDockerfile_AsyncCallback_Works()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        container.WithDockerfile("context", async context =>
        {
            await Task.Delay(10); // Simulate async work
            context.Builder.From("alpine", "latest")
                .Run("echo test");
        });

        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);

        var factoryContext = new DockerfileFactoryContext
        {
            Services = appBuilder.Services.BuildServiceProvider(),
            Resource = container.Resource,
            CancellationToken = CancellationToken.None
        };

        // Act
        var dockerfile = await buildAnnotation.DockerfileFactory!(factoryContext);

        // Assert
        Assert.Contains("FROM alpine:latest", dockerfile);
        Assert.Contains("RUN echo test", dockerfile);
    }

    [Fact]
    public async Task WithDockerfile_ContextProvidesServices()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        appBuilder.Services.AddSingleton<string>("test-config-value");
        
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        container.WithDockerfile("context", context =>
        {
            var config = context.Services.GetService<string>();
            context.Builder.From("alpine", "latest")
                .Env("CONFIG", config ?? "default");
        });

        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);

        var factoryContext = new DockerfileFactoryContext
        {
            Services = appBuilder.Services.BuildServiceProvider(),
            Resource = container.Resource,
            CancellationToken = CancellationToken.None
        };

        // Act
        var dockerfile = await buildAnnotation.DockerfileFactory!(factoryContext);

        // Assert
        Assert.Contains("ENV CONFIG=test-config-value", dockerfile);
    }

    [Fact]
    public async Task WithDockerfile_MultiStageDockerfile()
    {
        // Arrange
        var appBuilder = DistributedApplication.CreateBuilder();
        var container = appBuilder.AddContainer("mycontainer", "myimage");

        container.WithDockerfile("context", context =>
        {
            // Builder stage
            var builder = context.Builder.From("node", "18", "builder");
            builder.WorkDir("/build")
                .Copy("package*.json", "./")
                .Run("npm ci")
                .Copy(".", ".")
                .Run("npm run build");

            // Runtime stage
            var runtime = context.Builder.From("node", "18-alpine");
            runtime.WorkDir("/app")
                .CopyFrom("builder", "/build/dist", "./")
                .Cmd(["node", "index.js"]);
        });

        var buildAnnotation = container.Resource.Annotations.OfType<DockerfileBuildAnnotation>().LastOrDefault();
        Assert.NotNull(buildAnnotation);

        var factoryContext = new DockerfileFactoryContext
        {
            Services = appBuilder.Services.BuildServiceProvider(),
            Resource = container.Resource,
            CancellationToken = CancellationToken.None
        };

        // Act
        var dockerfile = await buildAnnotation.DockerfileFactory!(factoryContext);

        // Assert
        Assert.Contains("FROM node:18 AS builder", dockerfile);
        Assert.Contains("FROM node:18-alpine", dockerfile);
        Assert.Contains("COPY --from=builder /build/dist ./", dockerfile);
    }
}
