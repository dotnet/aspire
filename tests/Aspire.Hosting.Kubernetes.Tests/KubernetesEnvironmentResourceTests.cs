// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Utils;
using Xunit;

public class KubernetesEnvironmentResourceTests(ITestOutputHelper output)
{
    [Fact]
    public void PublishingDockerComposeEnviromentPublishesFile()
    {
        var tempDir = Directory.CreateTempSubdirectory(".k8s-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        builder.AddKubernetesEnvironment("env");

        // Add a container to the application
        builder.AddContainer("service", "nginx");

        var app = builder.Build();
        app.Run();

        var chartYaml = Path.Combine(tempDir.FullName, "Chart.yaml");
        var valuesYaml = Path.Combine(tempDir.FullName, "values.yaml");
        var deploymentYaml = Path.Combine(tempDir.FullName, "templates", "service", "deployment.yaml");
        Assert.True(File.Exists(chartYaml), "Chart.yaml file was not created.");
        Assert.True(File.Exists(valuesYaml), "values.yaml file was not created.");
        Assert.True(File.Exists(deploymentYaml), "Deployment.yaml file was not created.");

        tempDir.Delete(recursive: true);
    }
}
