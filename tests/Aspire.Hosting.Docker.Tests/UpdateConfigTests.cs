// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Docker.Resources;
using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

namespace Aspire.Hosting.Docker.Tests;

public class UpdateConfigTests
{
    [Fact]
    public void UpdateConfig_SerializesParallelismAsInteger()
    {
        // Arrange
        var updateConfig = new UpdateConfig
        {
            Parallelism = 2
        };

        var composeFile = new ComposeFile
        {
            Services = new Dictionary<string, Service>
            {
                ["test-service"] = new Service
                {
                    Name = "test-service",
                    Image = "nginx",
                    Deploy = new Deploy
                    {
                        UpdateConfig = updateConfig
                    }
                }
            }
        };

        // Act
        var yaml = composeFile.ToYaml();

        // Print for debugging
        Console.WriteLine("Generated YAML:");
        Console.WriteLine(yaml);

        // Assert - should serialize as integer (correct)
        Assert.Contains("parallelism: 2", yaml);
        Assert.DoesNotContain("parallelism: \"2\"", yaml);
    }

    [Fact]
    public void UpdateConfig_SerializesFailureActionAsString()
    {
        // Arrange
        var updateConfig = new UpdateConfig
        {
            FailureAction = "rollback"
        };

        var composeFile = new ComposeFile
        {
            Services = new Dictionary<string, Service>
            {
                ["test-service"] = new Service
                {
                    Name = "test-service",
                    Image = "nginx", 
                    Deploy = new Deploy
                    {
                        UpdateConfig = updateConfig
                    }
                }
            }
        };

        // Act
        var yaml = composeFile.ToYaml();

        // Assert - should serialize as string
        Assert.Contains("failure_action: rollback", yaml);
    }

    [Theory]
    [InlineData("continue")]
    [InlineData("rollback")]
    [InlineData("pause")]
    public void UpdateConfig_AcceptsValidFailureActionValues(string failureAction)
    {
        // Arrange & Act
        var updateConfig = new UpdateConfig
        {
            FailureAction = failureAction
        };

        // Assert - no exception should be thrown
        Assert.Equal(failureAction, updateConfig.FailureAction);
    }

    [Fact]
    public void UpdateConfig_SerializesCompleteConfiguration()
    {
        // Arrange
        var updateConfig = new UpdateConfig
        {
            Parallelism = 1,
            Delay = "10s",
            Monitor = "60s",
            Order = "start-first",
            FailureAction = "pause",
            MaxFailureRatio = "0.1"
        };

        var composeFile = new ComposeFile
        {
            Services = new Dictionary<string, Service>
            {
                ["web"] = new Service
                {
                    Name = "web",
                    Image = "nginx",
                    Deploy = new Deploy
                    {
                        UpdateConfig = updateConfig
                    }
                }
            }
        };

        // Act
        var yaml = composeFile.ToYaml();

        // Assert
        Assert.Contains("parallelism: 1", yaml);
        Assert.Contains("delay: 10s", yaml);
        Assert.Contains("monitor: 60s", yaml);
        Assert.Contains("order: start-first", yaml);
        Assert.Contains("failure_action: pause", yaml);
        Assert.Contains("max_failure_ratio: 0.1", yaml);
    }

    [Fact]
    public void UpdateConfig_ParallelismNullValueOmittedFromYaml()
    {
        // Arrange
        var updateConfig = new UpdateConfig
        {
            Parallelism = null, // Should be omitted from YAML
            Delay = "10s"
        };

        var composeFile = new ComposeFile
        {
            Services = new Dictionary<string, Service>
            {
                ["test-service"] = new Service
                {
                    Name = "test-service",
                    Image = "nginx",
                    Deploy = new Deploy
                    {
                        UpdateConfig = updateConfig
                    }
                }
            }
        };

        // Act
        var yaml = composeFile.ToYaml();

        // Print for debugging
        Console.WriteLine("Generated YAML:");
        Console.WriteLine(yaml);

        // Assert - null values should be omitted
        Assert.DoesNotContain("parallelism:", yaml);
        Assert.Contains("delay: 10s", yaml);
    }

    [Fact]
    public void UpdateConfig_FailureActionNullValueOmittedFromYaml()
    {
        // Arrange
        var updateConfig = new UpdateConfig
        {
            FailureAction = null, // Should be omitted from YAML
            Delay = "10s"
        };

        var composeFile = new ComposeFile
        {
            Services = new Dictionary<string, Service>
            {
                ["test-service"] = new Service
                {
                    Name = "test-service",
                    Image = "nginx",
                    Deploy = new Deploy
                    {
                        UpdateConfig = updateConfig
                    }
                }
            }
        };

        // Act
        var yaml = composeFile.ToYaml();

        // Assert - null values should be omitted
        Assert.DoesNotContain("failure_action:", yaml);
        Assert.Contains("delay: 10s", yaml);
    }
}