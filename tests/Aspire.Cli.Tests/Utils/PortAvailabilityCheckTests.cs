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

    [Fact]
    public void ParseExcludedPortRanges_WithTypicalNetshOutput_ParsesRanges()
    {
        var output = """

        Protocol tcp Port Exclusion Ranges

        Start Port    End Port
        ----------    --------
             1080        1179
             1180        1279
            50000       50099     *
            56224       56323

        * - Administered port exclusions.

        """;

        var ranges = PortAvailabilityCheck.ParseExcludedPortRanges(output);

        Assert.Equal(4, ranges.Count);
        Assert.Contains(ranges, r => r.Start == 1080 && r.End == 1179);
        Assert.Contains(ranges, r => r.Start == 1180 && r.End == 1279);
        Assert.Contains(ranges, r => r.Start == 50000 && r.End == 50099);
        Assert.Contains(ranges, r => r.Start == 56224 && r.End == 56323);
    }

    [Fact]
    public void ParseExcludedPortRanges_WithEmptyOutput_ReturnsEmpty()
    {
        var ranges = PortAvailabilityCheck.ParseExcludedPortRanges("");
        Assert.Empty(ranges);
    }

    [Fact]
    public void ParseExcludedPortRanges_WithHeadersOnly_ReturnsEmpty()
    {
        var output = """

        Protocol tcp Port Exclusion Ranges

        Start Port    End Port
        ----------    --------

        * - Administered port exclusions.

        """;

        var ranges = PortAvailabilityCheck.ParseExcludedPortRanges(output);
        Assert.Empty(ranges);
    }

    [Fact]
    public void ExtractPortsFromEnvironmentVariables_ExtractsUrlsAndEndpoints()
    {
        var envVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = "https://localhost:15000;http://localhost:15001",
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL"] = "https://localhost:15002",
            ["ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL"] = "https://localhost:15003"
        };

        var ports = PortAvailabilityCheck.ExtractPortsFromEnvironmentVariables(envVars);

        Assert.Equal(4, ports.Count);
        Assert.Contains(ports, p => p.Port == 15000 && p.Source == "applicationUrl");
        Assert.Contains(ports, p => p.Port == 15001 && p.Source == "applicationUrl");
        Assert.Contains(ports, p => p.Port == 15002 && p.Source == "ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL");
        Assert.Contains(ports, p => p.Port == 15003 && p.Source == "ASPIRE_RESOURCE_SERVICE_ENDPOINT_URL");
    }

    [Fact]
    public void ExtractPortsFromEnvironmentVariables_IgnoresNonUrlVars()
    {
        var envVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Development",
            ["DOTNET_ENVIRONMENT"] = "Development"
        };

        var ports = PortAvailabilityCheck.ExtractPortsFromEnvironmentVariables(envVars);
        Assert.Empty(ports);
    }

    [Fact]
    public void ParseDynamicPortRange_WithTypicalOutput_ParsesRange()
    {
        var output = """

        Protocol tcp Dynamic Port Range
        ---------------------------------
        Start Port      : 49152
        Number of Ports : 16384

        """;

        var range = PortAvailabilityCheck.ParseDynamicPortRange(output);

        Assert.NotNull(range);
        Assert.Equal(49152, range.Value.Start);
        Assert.Equal(65535, range.Value.End);
    }

    [Fact]
    public void ParseDynamicPortRange_WithEmptyOutput_ReturnsNull()
    {
        var range = PortAvailabilityCheck.ParseDynamicPortRange("");
        Assert.Null(range);
    }

    [Fact]
    public void ParseDynamicPortRange_WithCustomRange_ParsesCorrectly()
    {
        var output = """
        Protocol tcp Dynamic Port Range
        ---------------------------------
        Start Port      : 1024
        Number of Ports : 64511
        """;

        var range = PortAvailabilityCheck.ParseDynamicPortRange(output);

        Assert.NotNull(range);
        Assert.Equal(1024, range.Value.Start);
        Assert.Equal(65534, range.Value.End);
    }
}
