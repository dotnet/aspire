#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.TestUtilities;

public class KubernetesEnvironmentResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PublishingKubernetesEnvironmentPublishesFile()
    {
        var tempDir = Directory.CreateTempSubdirectory(".k8s-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.FullName);

        builder.AddKubernetesEnvironment("env");

        // Add a container to the application
        builder.AddContainer("service", "nginx");

        var app = builder.Build();
        app.Run();

        var chartYaml = Path.Combine(tempDir.FullName, "Chart.yaml");
        var valuesYaml = Path.Combine(tempDir.FullName, "values.yaml");
        var deploymentYaml = Path.Combine(tempDir.FullName, "templates", "service", "deployment.yaml");

        await Verify(File.ReadAllText(chartYaml), "yaml")
            .AppendContentAsFile(File.ReadAllText(valuesYaml), "yaml")
            .AppendContentAsFile(File.ReadAllText(deploymentYaml), "yaml");

        tempDir.Delete(recursive: true);
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11818", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
    public async Task PublishAsKubernetesService_ThrowsIfNoEnvironment()
    {
        static async Task RunTest(Action<IDistributedApplicationBuilder> action)
        {
            var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);
            // Do not add AddKubernetesEnvironment

            action(builder);

            using var app = builder.Build();

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ExecuteBeforeStartHooksAsync(app, default));

            Assert.Contains("there are no 'KubernetesEnvironmentResource' resources", ex.Message);
        }

        await RunTest(builder =>
            builder.AddProject<Projects.ServiceA>("ServiceA")
                .PublishAsKubernetesService((_) => { }));

        await RunTest(builder =>
            builder.AddContainer("api", "myimage")
                .PublishAsKubernetesService((_) => { }));

        await RunTest(builder =>
            builder.AddExecutable("exe", "path/to/executable", ".")
                .PublishAsDockerFile()
                .PublishAsKubernetesService((_) => { }));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/aspire/issues/11818", typeof(PlatformDetection), nameof(PlatformDetection.IsRunningFromAzdo))]
    public async Task MultipleKubernetesEnvironmentsSupported()
    {
        using var tempDir = new TempDirectory();

        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, tempDir.Path);

        var env1 = builder.AddKubernetesEnvironment("env1");
        var env2 = builder.AddKubernetesEnvironment("env2");

        builder.AddProject<Projects.ServiceA>("ServiceA")
            .WithComputeEnvironment(env1);

        builder.AddProject<Projects.ServiceB>("ServiceB")
            .WithComputeEnvironment(env2);

        using var app = builder.Build();

        // Publishing will stop the app when it is done
        await app.RunAsync();

        await VerifyDirectory(tempDir.Path);
    }

    [Fact]
    public async Task GetHostAddressExpression()
    {
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        var env = builder.AddKubernetesEnvironment("env");

        var project = builder
            .AddProject<Projects.ServiceA>("project1", launchProfileName: null)
            .WithHttpEndpoint();

        var endpointReferenceEx = ((IComputeEnvironmentResource)env.Resource).GetHostAddressExpression(project.GetEndpoint("http"));
        Assert.NotNull(endpointReferenceEx);

        Assert.Equal("project1-service", endpointReferenceEx.Format);
        Assert.Empty(endpointReferenceEx.ValueProviders);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ExecuteBeforeStartHooksAsync")]
    private static extern Task ExecuteBeforeStartHooksAsync(DistributedApplication app, CancellationToken cancellationToken);
}
