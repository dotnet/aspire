// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE002
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Aspire.Hosting.Docker.Tests;

public class DockerComposeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DockerComposeSetsComputeEnvironment()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        var composeEnv = builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container to the application
        var container = builder.AddContainer("service", "nginx");

        var app = builder.Build();

        await ExecuteBeforeStartHooksAsync(app, default);

        Assert.Same(composeEnv.Resource, container.Resource.GetDeploymentTargetAnnotation()?.ComputeEnvironment);
    }

    [Fact]
    public void PublishingDockerComposeEnviromentPublishesFile()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.FullName);

        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container to the application
        builder.AddContainer("service", "nginx");

        var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.FullName, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task DockerComposeOnlyExposesExternalEndpoints()
    {
        var tempDir = Directory.CreateTempSubdirectory(".docker-compose-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.FullName);

        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("docker-compose");

        // Add a container with both external and non-external endpoints
        builder.AddContainer("service", "nginx")
               .WithEndpoint(scheme: "http", port: 8080, name: "internal")  // Non-external endpoint
               .WithEndpoint(scheme: "http", port: 8081, name: "external", isExternal: true); // External endpoint

        var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.FullName, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");

        var composeContent = File.ReadAllText(composeFile);

        await Verify(composeContent, "yaml");

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task PublishAsDockerComposeService_ThrowsIfNoEnvironment()
    {
        static async Task RunTest(Action<IDistributedApplicationBuilder> action)
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
            // Do not add AddDockerComposeEnvironment

            action(builder);

            using var app = builder.Build();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

            Assert.Contains("there are no 'DockerComposeEnvironmentResource' resources", ex.Message);
        }

        await RunTest(builder =>
            builder.AddProject<Projects.ServiceA>("ServiceA")
                .PublishAsDockerComposeService((_, _) => { }));

        await RunTest(builder =>
            builder.AddContainer("api", "myimage")
                .PublishAsDockerComposeService((_, _) => { }));

        await RunTest(builder =>
            builder.AddExecutable("exe", "path/to/executable", ".")
                .PublishAsDockerFile()
                .PublishAsDockerComposeService((_, _) => { }));
    }

    [Fact]
    public async Task MultipleDockerComposeEnvironmentsSupported()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        var env1 = builder.AddDockerComposeEnvironment("env1");
        var env2 = builder.AddDockerComposeEnvironment("env2");

        builder.AddContainer("api1", "myimage")
            .WithComputeEnvironment(env1);

        builder.AddContainer("api2", "myimage")
            .WithComputeEnvironment(env2);

        using var app = builder.Build();

        // Publishing will stop the app when it is done
        await app.RunAsync();

        await VerifyDirectory(tempDir.Path);
    }

    [Fact]
    public async Task DashboardWithForwardedHeadersWritesEnvVar()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("env")
               .WithDashboard(d => d.WithForwardedHeaders());

        // Add a sample service to force compose generation
        builder.AddContainer("api", "myimage");

        using var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");
        var composeContent = File.ReadAllText(composeFile);

        await Verify(composeContent, "yaml");
    }

    [Fact]
    public async Task DockerSwarmDeploymentLabelsSerializedCorrectly()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.Path);
        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        builder.AddDockerComposeEnvironment("swarm-env");

        // Add a service with Docker Swarm deployment labels
        builder.AddContainer("my-service", "my-image:latest")
            .PublishAsDockerComposeService((resource, service) =>
            {
                service.Deploy = new Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm.Deploy
                {
                    Labels = new Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm.LabelSpecs
                    {
                        ["com.example.foo"] = "bar",
                        ["com.example.env"] = "production"
                    }
                };
            });

        using var app = builder.Build();
        app.Run();

        var composeFile = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composeFile), "Docker Compose file was not created.");
        var composeContent = File.ReadAllText(composeFile);

        // Verify the deployment labels are serialized as direct key-value pairs
        // instead of nested under "additional_labels"
        await Verify(composeContent, "yaml");
    }

    [Fact]
    public async Task GetHostAddressExpression()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddDockerComposeEnvironment("env");

        var project = builder
            .AddProject<Projects.ServiceA>("Project1", launchProfileName: null)
            .WithHttpEndpoint();

        var endpointReferenceEx = ((IComputeEnvironmentResource)env.Resource).GetHostAddressExpression(project.GetEndpoint("http"));
        Assert.NotNull(endpointReferenceEx);

        Assert.Equal("project1", endpointReferenceEx.Format);
        Assert.Empty(endpointReferenceEx.ValueProviders);
    }

    private sealed class MockImageBuilder : IResourceContainerImageManager
    {
        public bool BuildImageCalled { get; private set; }

        public Task BuildImageAsync(IResource resource, CancellationToken cancellationToken = default)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }

        public Task BuildImagesAsync(IEnumerable<IResource> resources, CancellationToken cancellationToken = default)
        {
            BuildImageCalled = true;
            return Task.CompletedTask;
        }

        public Task PushImageAsync(IResource resource, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void DockerComposeProjectNameIncludesAppHostShaInArguments()
    {
        using var tempDir = new TempDirectory();
        var testSink = new TestSink();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.Path, step: WellKnownPipelineSteps.Deploy);

        // Add TestLoggerProvider to capture logs during publish, set minimum level to Debug
        builder.Services.AddLogging(logging =>
        {
            logging.AddProvider(new TestLoggerProvider(testSink));
            logging.SetMinimumLevel(LogLevel.Debug);
        });

        // Set a known AppHost SHA in configuration
        const string testSha = "ABC123DEF456789ABCDEF123456789ABCDEF123456789ABCDEF123456789ABC";
        builder.Configuration["AppHost:PathSha256"] = testSha;

        builder.Services.AddSingleton<IResourceContainerImageManager, MockImageBuilder>();

        var composeEnv = builder.AddDockerComposeEnvironment("my-environment");
        builder.AddContainer("service", "nginx");

        var app = builder.Build();

        app.Run();

        // Verify that docker-compose.yaml was created
        var composePath = Path.Combine(tempDir.Path, "docker-compose.yaml");
        Assert.True(File.Exists(composePath));

        // Check for docker compose up command with project name
        var expectedProjectName = "aspire-my-environment-abc123de";

        // Check for docker compose up command with project name
        var logMessages = testSink.Writes.Select(w => w.Message).ToList();
        Assert.Contains(logMessages, msg =>
            msg != null &&
            msg.Contains("compose", StringComparison.OrdinalIgnoreCase) &&
            msg.Contains("--project-name", StringComparison.OrdinalIgnoreCase) &&
            msg.Contains(expectedProjectName, StringComparison.OrdinalIgnoreCase) &&
            msg.Contains("up", StringComparison.OrdinalIgnoreCase));
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
