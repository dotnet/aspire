// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.Utils;

public class KubernetesEnvironmentResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task PublishingDockerComposeEnviromentPublishesFile()
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

        await Verify(File.ReadAllText(chartYaml), "yaml")
            .AppendContentAsFile(File.ReadAllText(valuesYaml), "yaml")
            .AppendContentAsFile(File.ReadAllText(deploymentYaml), "yaml");

        tempDir.Delete(recursive: true);
    }
}
