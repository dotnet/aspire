// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureEnvironmentResourceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task WhenUsedWithAzureContainerAppsEnvironment_GeneratesProperBicep()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Add a container that will use the container app environment
        builder.AddContainer("api", "my-api-image:latest")
            .WithHttpEndpoint();

        // Act
        using var app = builder.Build();
        app.Run();

        var mainBicep = File.ReadAllText(Path.Combine(tempDir.FullName, "main.bicep"));
        var envBicep = File.ReadAllText(Path.Combine(tempDir.FullName, "env", "env.bicep"));

        await Verifier.Verify(mainBicep, "bicep")
            .AppendContentAsFile(envBicep, "bicep")
            .UseHelixAwareDirectory();

        tempDir.Delete(recursive: true);
    }

    [Fact]
    public async Task WhenUsedWithAzureContainerAppsEnvironment_RespectsWithProperties()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory(".azure-environment-resource-test");
        output.WriteLine($"Temp directory: {tempDir.FullName}");
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish, publisher: "default", outputPath: tempDir.FullName);

        builder.AddAzureEnvironment()
            .WithProperties(env =>
            {
                env.Location = "East US";
                env.ResourceGroupName = "my-env";
            });
        var containerAppEnv = builder.AddAzureContainerAppEnvironment("env");

        // Add a container that will use the container app environment
        builder.AddContainer("api", "my-api-image:latest")
            .WithHttpEndpoint();

        // Act
        using var app = builder.Build();
        app.Run();

        var mainBicep = File.ReadAllText(Path.Combine(tempDir.FullName, "main.bicep"));

        await Verifier.Verify(mainBicep, "bicep")
            .UseHelixAwareDirectory();

        tempDir.Delete(recursive: true);
    }
}
