// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils.EnvironmentChecker;

namespace Aspire.Cli.Tests.Utils;

public class PortAvailabilityCheckTests
{
    [Fact]
    public void ReadConfiguredPorts_WithValidApphostRunJson_ReturnsPorts()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory("aspire-port-test-");
        try
        {
            var json = """
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:15000;http://localhost:15001",
                  "environmentVariables": {
                    "ASPNETCORE_ENVIRONMENT": "Development",
                    "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL": "https://localhost:15002",
                    "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL": "https://localhost:15003"
                  }
                }
              }
            }
            """;
            File.WriteAllText(Path.Combine(tempDir.FullName, "apphost.run.json"), json);

            // Act
            var ports = PortAvailabilityCheck.ReadConfiguredPorts(tempDir);

            // Assert
            Assert.Contains(ports, p => p.Port == 15000 && p.Source == "applicationUrl");
            Assert.Contains(ports, p => p.Port == 15001 && p.Source == "applicationUrl");
            Assert.Contains(ports, p => p.Port == 15002 && p.Source == "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL");
            Assert.Contains(ports, p => p.Port == 15003 && p.Source == "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL");
            Assert.Equal(4, ports.Count);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public void ReadConfiguredPorts_WithNoConfigFile_ReturnsEmpty()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory("aspire-port-test-");
        try
        {
            // Act
            var ports = PortAvailabilityCheck.ReadConfiguredPorts(tempDir);

            // Assert
            Assert.Empty(ports);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public void ReadConfiguredPorts_WithLaunchSettings_ReturnsPorts()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory("aspire-port-test-");
        try
        {
            var propsDir = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "Properties"));
            var json = """
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:5001;http://localhost:5000"
                }
              }
            }
            """;
            File.WriteAllText(Path.Combine(propsDir.FullName, "launchSettings.json"), json);

            // Act
            var ports = PortAvailabilityCheck.ReadConfiguredPorts(tempDir);

            // Assert
            Assert.Contains(ports, p => p.Port == 5001);
            Assert.Contains(ports, p => p.Port == 5000);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public void ReadConfiguredPorts_PrefersApphostRunJson_OverLaunchSettings()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory("aspire-port-test-");
        try
        {
            // Create both files with different ports
            File.WriteAllText(Path.Combine(tempDir.FullName, "apphost.run.json"), """
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:9999"
                }
              }
            }
            """);

            var propsDir = Directory.CreateDirectory(Path.Combine(tempDir.FullName, "Properties"));
            File.WriteAllText(Path.Combine(propsDir.FullName, "launchSettings.json"), """
            {
              "profiles": {
                "https": {
                  "applicationUrl": "https://localhost:8888"
                }
              }
            }
            """);

            // Act
            var ports = PortAvailabilityCheck.ReadConfiguredPorts(tempDir);

            // Assert - should use apphost.run.json
            Assert.Contains(ports, p => p.Port == 9999);
            Assert.DoesNotContain(ports, p => p.Port == 8888);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }

    [Fact]
    public void ReadConfiguredPorts_FallsBackToFirstProfile_WhenNoHttpsProfile()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory("aspire-port-test-");
        try
        {
            File.WriteAllText(Path.Combine(tempDir.FullName, "apphost.run.json"), """
            {
              "profiles": {
                "default": {
                  "applicationUrl": "http://localhost:7777"
                }
              }
            }
            """);

            // Act
            var ports = PortAvailabilityCheck.ReadConfiguredPorts(tempDir);

            // Assert
            Assert.Contains(ports, p => p.Port == 7777);
        }
        finally
        {
            tempDir.Delete(true);
        }
    }
}
