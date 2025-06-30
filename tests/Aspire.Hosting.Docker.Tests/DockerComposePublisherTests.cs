// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;

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
                    .WithContainerFiles("/usr/local/share", [
                        new ContainerFile
                        {
                            Name = "redis.conf",
                            Contents = "hello world",
                        },
                        new ContainerDirectory
                        {
                            Name = "folder",
                            Entries = [
                                new ContainerFile
                                {
                                    Name = "file.sh",
                                    SourcePath = "./hello.sh",
                                    Owner = 1000,
                                    Group = 1000,
                                    Mode = UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead,
                                },
                            ],
                        },
                    ])
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
                         .WaitFor(param0)
                         .WithOtlpExporter();

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
            .AppendContentAsFile(File.ReadAllText(envPath), "env");
    }

    [Fact]
    public async Task DockerComposeWithProjectResources()
    {
        using var tempDir = new TempDirectory();
        // Arrange

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a project
        var project = builder.AddProject<TestProjectWithLaunchSettings>("project1");

        builder.AddContainer("api", "reg:api")
               .WithReference(project);

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        var envPath = Path.Combine(tempDir.Path, ".env");
        Assert.True(File.Exists(composePath));
        Assert.True(File.Exists(envPath));

        await Verify(File.ReadAllText(composePath), "yaml")
            .AppendContentAsFile(File.ReadAllText(envPath), "env");
    }

    [Fact]
    public async Task DockerComposeCorrectlyEmitsPortMappings()
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path)
            .WithTestAndResourceLogging(outputHelper);
        
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        builder.AddContainer("resource", "mcr.microsoft.com/dotnet/aspnet:8.0")
               .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
               .WithHttpEndpoint(env: "HTTP_PORT");

        var app = builder.Build();

        await app.RunAsync().WaitAsync(TimeSpan.FromSeconds(60));

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        await Verify(File.ReadAllText(composePath), "yaml");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DockerComposeHandleImageBuilding(bool shouldBuildImages)
    {
        using var tempDir = new TempDirectory();
        using var builder = TestDistributedApplicationBuilder.Create(["--operation", "publish", "--publisher", "default", "--output-path", tempDir.Path])
            .WithTestAndResourceLogging(outputHelper);
        
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

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
            .AppendContentAsFile(File.ReadAllText(envPath), "env");
    }

    [Fact]
    public async Task DockerComposeDoesNotOverwriteEnvFileOnPublish()
    {
        using var tempDir = new TempDirectory();
        var envFilePath = Path.Combine(tempDir.Path, ".env");

        void PublishApp()
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
            builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

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
            .AppendContentAsFile(secondContent, "env");
    }

    [Fact]
    public async Task DockerComposeAppendsNewKeysToEnvFileOnPublish()
    {
        using var tempDir = new TempDirectory();
        var envFilePath = Path.Combine(tempDir.Path, ".env");

        void PublishApp(params string[] paramNames)
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
            builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

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
            .AppendContentAsFile(secondContent, "env");
    }

    [Fact]
    public async Task DockerComposeMapsPortsProperly()
    {
        using var tempDir = new TempDirectory();
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(false);

        var container = builder.AddExecutable("service", "foo", ".")
            .PublishAsDockerFile()
            .WithHttpEndpoint(env: "PORT");

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeFile = File.ReadAllText(composePath);

        await Verify(composeFile);
    }

    [Fact]
    public async Task PublishAsync_WithDashboardEnabled_IncludesDashboardService()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(); // Dashboard enabled by default

        // Add a container with OTLP exporter
        builder.AddContainer("api", "my-api")
            .WithOtlpExporter();

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeContent = File.ReadAllText(composePath);

        await Verify(composeContent, "yaml");
    }

    [Fact]
    public async Task PublishAsync_WithDashboardDisabled_DoesNotIncludeDashboardService()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(false);

        // Add a container with OTLP exporter
        builder.AddContainer("api", "my-api")
            .WithOtlpExporter();

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeContent = File.ReadAllText(composePath);

        await Verify(composeContent, "yaml");
    }

    [Fact]
    public async Task PublishAsync_WithDashboard_UsesCustomConfiguration()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(dashboard =>
            {
                dashboard.WithImage("custom-dashboard:latest")
                    .WithContainerName("custom-dashboard")
                    .WithEnvironment("CUSTOM_VAR", "custom-value")
                    .WithHostPort(8081);
            });

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeContent = File.ReadAllText(composePath);

        await Verify(composeContent, "yaml");
    }

    [Fact]
    public async Task PublishAsync_MultipleResourcesWithOtlp_ConfiguresAllForDashboard()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard();

        // Add multiple containers with OTLP exporter
        builder.AddContainer("api", "my-api")
            .WithOtlpExporter();

        builder.AddContainer("worker", "my-worker") 
            .WithOtlpExporter();

        // Add a container without OTLP - should not be configured
        builder.AddContainer("database", "postgres");

        var app = builder.Build();
        app.Run();

        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        var composeContent = File.ReadAllText(composePath);

        await Verify(composeContent, "yaml");
    }

    [Fact]
    public void PublishAsync_InRunMode_DoesNotCreateDashboard()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run, outputPath: tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose")
            .WithDashboard(); // Should be ignored in run mode

        builder.AddContainer("api", "my-api")
            .WithOtlpExporter();

        var app = builder.Build();

        // In run mode, no compose file should be generated
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.False(File.Exists(composePath));
    }

    [Fact]
    public async Task DockerComposeHandlesBindMounts()
    {
        using var tempDir = new TempDirectory();
        
        // Create a temporary file to use as a bind mount source
        using var sourceDir = new TempDirectory();
        var sourceFile = Path.Combine(sourceDir.Path, "test.txt");
        await File.WriteAllTextAsync(sourceFile, "Hello, World!");
        
        var sourceDirectory = Path.Combine(sourceDir.Path, "testdir");
        Directory.CreateDirectory(sourceDirectory);
        var nestedFile = Path.Combine(sourceDirectory, "nested.txt");
        await File.WriteAllTextAsync(nestedFile, "Nested content");

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.Path);

        builder.Services.AddSingleton<IResourceContainerImageBuilder, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        builder.AddContainer("api", "my-api")
               .WithBindMount(sourceFile, "/app/test.txt")
               .WithBindMount(sourceDirectory, "/app/testdir")
               .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock"); // Docker socket - should pass through

        var app = builder.Build();

        // Act
        app.Run();

        // Assert
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        // Verify that the bind mount sources were copied to the output directory (but not docker socket)
        var copiedFile = Path.Combine(tempDir.Path, "api", "bindmounts", "test.txt");
        var copiedDirectory = Path.Combine(tempDir.Path, "api", "bindmounts", "testdir");
        var copiedNestedFile = Path.Combine(copiedDirectory, "nested.txt");

        Assert.True(File.Exists(copiedFile));
        Assert.True(Directory.Exists(copiedDirectory));
        Assert.True(File.Exists(copiedNestedFile));

        // Docker socket should NOT be copied
        var dockerSocketCopy = Path.Combine(tempDir.Path, "api", "bindmounts", "docker.sock");
        Assert.False(File.Exists(dockerSocketCopy));

        // Verify file contents
        Assert.Equal("Hello, World!", await File.ReadAllTextAsync(copiedFile));
        Assert.Equal("Nested content", await File.ReadAllTextAsync(copiedNestedFile));

        // Verify the compose file uses relative paths for regular bind mounts and original path for docker socket
        var composeContent = await File.ReadAllTextAsync(composePath);
        await Verify(composeContent, "yaml");
    }

    private sealed class MockImageBuilder : IResourceContainerImageBuilder
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }

        public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class TestProject : IProjectMetadata
    {
        public string ProjectPath => "another-path";

        public LaunchSettings? LaunchSettings { get; set; }
    }

    private sealed class TestProjectWithLaunchSettings : IProjectMetadata
    {
        public Dictionary<string, LaunchProfile>? Profiles { get; set; } = [];
        public string ProjectPath => "another-path";
        public LaunchSettings? LaunchSettings => new() { Profiles = Profiles! };

        public TestProjectWithLaunchSettings() => Profiles = new()
        {
            ["https"] = new()
            {
                CommandName = "Project",
                LaunchBrowser = true,
                ApplicationUrl = "http://localhost:5031;https://localhost:5032",
                EnvironmentVariables = new()
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                }
            },
            ["http"] = new()
            {
                CommandName = "Project",
                LaunchBrowser = true,
                ApplicationUrl = "http://localhost:5031",
                EnvironmentVariables = new()
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development"
                }
            }
        };
    }
}
