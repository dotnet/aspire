// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Docker.Resources.ComposeNodes;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposePublisherTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public async Task PublishAsync_GeneratesValidDockerComposeFile()
    {
        using var tempDir = new TempDirectory();
        // Arrange

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        var param0 = builder.AddParameter("param0");
        var param1 = builder.AddParameter("param1", secret: true);
        var param2 = builder.AddParameter("param2", "default", publishValueAsDefault: true);
        var cs = builder.AddConnectionString("cs", ReferenceExpression.Create($"Url={param0}, Secret={param1}"));

        // Add a container to the application
        var redis = builder.AddContainer("cache", "redis")
                    .WithEntrypoint("/bin/sh")
                    .WithHttpEndpoint(name: "h2", port: 5000, targetPort: 5001)
                    .WithHttpEndpoint(env: "REDIS_PORT")
                    .WithArgs("-c", "hello $MSG")
                    .WithEnvironment("MSG", "world")
                    .WithEnvironment(context =>
                    {
                        var resource = (IResourceWithEndpoints)context.Resource;

                        context.EnvironmentVariables["TP"] = resource.GetEndpoint("http").Property(EndpointProperty.TargetPort);
                        context.EnvironmentVariables["TPH2"] = resource.GetEndpoint("h2").Property(EndpointProperty.TargetPort);
                    });

        var migration = builder.AddContainer("something", "dummy/migration:latest")
                         .WithContainerName("cn");

        var api = builder.AddContainer("myapp", "mcr.microsoft.com/dotnet/aspnet:8.0")
                         .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                         .WithHttpEndpoint(env: "PORT")
                         .WithEnvironment("param0", param0)
                         .WithEnvironment("param1", param1)
                         .WithEnvironment("param2", param2)
                         .WithReference(cs)
                         .WithArgs("--cs", cs.Resource)
                         .WaitFor(redis)
                         .WaitForCompletion(migration)
                         .WaitFor(param0);

        builder.AddProject(
            "project1",
            "..\\TestingAppHost1\\TestingAppHost1.MyWebApp\\TestingAppHost1.MyWebApp.csproj",
            launchProfileName: null)
            .WithReference(api.GetEndpoint("http"));

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        var envPath = Path.Combine(tempDir.Path, ".env");
        Assert.True(File.Exists(composePath));
        Assert.True(File.Exists(envPath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .AppendContentAsFile(File.ReadAllText(envPath), "env")
            
    }

    [Fact]
    public async Task DockerComposeCorrectlyEmitsPortMappings()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path)
            .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose");

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
               .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
               .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        await app.RunAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        await Verify(File.ReadAllText(composePath), "yaml")
            
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DockerComposeHandleImageBuilding(bool shouldBuildImages)
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(["--operation", "publish", "--publisher", "default", "--output-path", tempDir.Path])
            .WithTestAndResourceLogging(outputHelper);

        builder.AddDockerComposeEnvironment("docker-compose")
               .WithProperties(e => e.BuildContainerImages = shouldBuildImages);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        var mockImageBuilder = app.Services.GetRequiredService<IResourceContainerImageBuilder>() as MockImageBuilder;

        Assert.NotNull(mockImageBuilder);

        // Act
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));
        Assert.Equal(shouldBuildImages, mockImageBuilder.BuildImageCalled);
    }

    [Fact]
    public async Task DockerComposeAppliesServiceCustomizations()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        var containerNameParam = builder.AddParameter("param-1", "default-name", publishValueAsDefault: true);

        builder.AddDockerComposeEnvironment("docker-compose")
               .WithProperties(e => e.DefaultNetworkName = "default-network")
               .ConfigureComposeFile(file =>
               {
                   file.AddNetwork(new Network { Name = "custom-network", Driver = "host" });

                   file.Name = "my application";
               });

        // Add a container to the application
        var container = builder.AddContainer("service", "nginx")
            .WithEnvironment("ORIGINAL_ENV", "value")
            .PublishAsDockerComposeService((serviceResource, composeService) =>
            {
                // Add a custom label
                composeService.Labels["custom-label"] = "test-value";

                // Add a custom environment variable
                composeService.AddEnvironmentalVariable("CUSTOM_ENV", "custom-value");

                // Set a restart policy
                composeService.Restart = "always";

                composeService.ContainerName = containerNameParam.AsEnvironmentPlaceholder(serviceResource);

                // Add a custom network
                composeService.Networks.Add("custom-network");
            });

        var app = builder.Build();

        app.Run();

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));
        var envPath = Path.Combine(tempDir.Path, ".env");
        Assert.True(File.Exists(envPath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .AppendContentAsFile(File.ReadAllText(envPath), "env")
            
    }

    [Fact]
    public async Task DockerComposeDoesNotOverwriteEnvFileOnPublish()
    {
        using var tempDir = new TempDirectory();
        var envFilePath = Path.Combine(tempDir.Path, ".env");

        void PublishApp()
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
            builder.AddDockerComposeEnvironment("docker-compose");
            var param = builder.AddParameter("param1");
            builder.AddContainer("app", "busybox").WithEnvironment("param1", param);
            var app = builder.Build();
            app.Run();
        }

        PublishApp();
        Assert.True(File.Exists(envFilePath));
        var firstContent = File.ReadAllText(envFilePath).Replace("PARAM1=", "PARAM1=changed");
        File.WriteAllText(envFilePath, firstContent);

        PublishApp();
        Assert.True(File.Exists(envFilePath));
        var secondContent = File.ReadAllText(envFilePath);

        await Verify(firstContent, "env")
            .AppendContentAsFile(secondContent, "env")
            
    }

    [Fact]
    public async Task DockerComposeAppendsNewKeysToEnvFileOnPublish()
    {
        using var tempDir = new TempDirectory();
        var envFilePath = Path.Combine(tempDir.Path, ".env");

        void PublishApp(params string[] paramNames)
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
            builder.AddDockerComposeEnvironment("docker-compose");

            var parmeters = paramNames.Select(name => builder.AddParameter(name).Resource).ToArray();

            builder.AddContainer("app", "busybox")
                    .WithEnvironment(context =>
                    {
                        foreach (var param in parmeters)
                        {
                            context.EnvironmentVariables[param.Name] = param;
                        }
                    });

            var app = builder.Build();
            app.Run();
        }

        PublishApp(["param1"]);
        Assert.True(File.Exists(envFilePath));
        var firstContent = File.ReadAllText(envFilePath).Replace("PARAM1=", "PARAM1=changed");
        File.WriteAllText(envFilePath, firstContent);

        PublishApp(["param1", "param2"]);
        Assert.True(File.Exists(envFilePath));
        var secondContent = File.ReadAllText(envFilePath);

        await Verify(firstContent, "env")
            .AppendContentAsFile(secondContent, "env")
            
    }

    [Fact]
    public async Task DockerComposeMapsPortsProperly()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        var container = builder.AddExecutable("service", "foo", ".")
            .PublishAsDockerFile()
            .WithHttpEndpoint(env: "PORT");

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeFile = File.ReadAllText(composePath);

        await Verify(composeFile)
            
    }

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = Directory.CreateTempSubdirectory(".aspire-compose").FullName;
        }

        public string Path { get; }
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }
}
